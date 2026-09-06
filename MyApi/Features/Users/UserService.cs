using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;
using MyApi.Auth;
using MyApi.Data;

namespace MyApi.Features.Users;

/// <summary>
/// Logic nghiệp vụ của user. Nói chuyện thẳng với <see cref="IMongoCollection{T}"/> —
/// không bọc thêm một tầng Repository nữa: driver Mongo ĐÃ LÀ repository
/// (Find/Insert/Update/Delete + LINQ), bọc lại chỉ để chép y nguyên chữ ký hàm.
///
/// <para>
/// Cú pháp <c>UserService(MongoContext context, ...)</c> ngay sau tên class là
/// <b>primary constructor</b> (C# 12): tham số được DI inject và dùng được thẳng
/// trong thân class, khỏi phải khai báo field rồi gán thủ công.
/// </para>
/// </summary>
public class UserService(MongoContext context, IPasswordHasher<User> passwordHasher) : IUserService
{
    public const string CollectionName = "users";

    /// <summary>Mã lỗi của Mongo khi vi phạm unique index.</summary>
    private const int DuplicateKeyErrorCode = 11000;

    private readonly IMongoCollection<User> _users = context.GetCollection<User>(CollectionName);

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users
            .Find(Builders<User>.Filter.Empty)
            .SortByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);

        return users.Select(ToResponse).ToList();
    }

    public async Task<UserResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync(cancellationToken);

        return user is null ? null : ToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        // Kiểm tra trước CHỈ để trả lỗi đẹp cho Admin FE. Nó không đảm bảo gì cả:
        // hai request song song đều thấy "chưa tồn tại" rồi cùng insert.
        // Thứ thật sự chặn trùng là unique index — xem UsersIndexInitializer.
        if (await _users.Find(u => u.Email == email).AnyAsync(cancellationToken))
        {
            throw new DuplicateEmailException(email);
        }

        var user = new User
        {
            Email = email,
            Role = UserRoles.User,
            DeviceId = null,          // chưa gắn thiết bị — chờ user login lần đầu
            DeviceBoundAt = null,
            ExpiredAt = request.ExpiredAt.UtcDateTime,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
        };

        // HashPassword tự sinh salt ngẫu nhiên rồi nhúng vào chuỗi kết quả,
        // nên hai user cùng mật khẩu vẫn ra hash khác nhau. Không cần cột salt riêng.
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        try
        {
            await _users.InsertOneAsync(user, options: null, cancellationToken);
        }
        catch (MongoWriteException exception) when (IsDuplicateKey(exception))
        {
            // Nhánh này chạy khi hai request cùng tạo một email trong tích tắc.
            throw new DuplicateEmailException(email);
        }

        // Sau InsertOne, driver đã gán ngược Id vào entity.
        return ToResponse(user);
    }

    public async Task<UserResponse?> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        var email = NormalizeEmail(request.Email);

        var update = Builders<User>.Update
            .Set(u => u.Email, email)
            .Set(u => u.Role, request.Role)
            .Set(u => u.ExpiredAt, request.ExpiredAt.UtcDateTime)
            .Set(u => u.IsActive, request.IsActive)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        try
        {
            var updated = await _users.FindOneAndUpdateAsync(
                Builders<User>.Filter.Eq(u => u.Id, id),
                update,
                new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After },
                cancellationToken);

            return updated is null ? null : ToResponse(updated);
        }
        catch (MongoCommandException exception) when (exception.Code == DuplicateKeyErrorCode)
        {
            throw new DuplicateEmailException(email);
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        var result = await _users.DeleteOneAsync(u => u.Id == id, cancellationToken);

        return result.DeletedCount > 0;
    }

    public async Task<bool> ResetDeviceAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        var result = await _users.UpdateOneAsync(
            Builders<User>.Filter.Eq(u => u.Id, id),
            Builders<User>.Update
                .Set(u => u.DeviceId, (string?)null)
                .Set(u => u.DeviceBoundAt, (DateTime?)null)
                .Set(u => u.UpdatedAt, DateTime.UtcNow),
            options: null,
            cancellationToken);

        // MatchedCount chứ không phải ModifiedCount: user đã ở trạng thái DeviceId = null
        // thì không có gì để sửa, ModifiedCount = 0 dù user tồn tại và thao tác vẫn đúng ý.
        return result.MatchedCount > 0;
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);

        return _users.Find(u => u.Email == normalized).FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task<User?> FindEntityByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return Task.FromResult<User?>(null);
        }

        return _users.Find(u => u.Id == id).FirstOrDefaultAsync(cancellationToken)!;
    }

    public async Task<bool> TryBindDeviceAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(userId, out _))
        {
            return false;
        }

        // Điều kiện "DeviceId == null" nằm TRONG filter, không phải trong một câu if
        // ở tầng C#. Đọc rồi mới ghi thì hai thiết bị đăng nhập cùng lúc sẽ cùng đọc
        // được null và cùng ghi đè — cả hai đều tưởng mình thắng.
        var result = await _users.UpdateOneAsync(
            Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.Eq(u => u.DeviceId, null)),
            Builders<User>.Update
                .Set(u => u.DeviceId, deviceId)
                .Set(u => u.DeviceBoundAt, DateTime.UtcNow)
                .Set(u => u.UpdatedAt, DateTime.UtcNow),
            options: null,
            cancellationToken);

        // MatchedCount = 0 nghĩa là filter không khớp: hoặc user không tồn tại,
        // hoặc DeviceId đã khác null — tức có thiết bị khác giữ chỗ rồi.
        return result.MatchedCount > 0;
    }

    /// <summary>
    /// Chuẩn hóa email trước khi lưu/so sánh. Phải gọi ở MỌI chỗ chạm tới email,
    /// nếu không unique index sẽ cho lọt "A@x.com" lẫn "a@x.com".
    /// </summary>
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsDuplicateKey(MongoWriteException exception) =>
        exception.WriteError?.Category == ServerErrorCategory.DuplicateKey;

    /// <summary>Chuyển entity sang DTO trả về.</summary>
    private static UserResponse ToResponse(User user) => new(
        user.Id!,
        user.Email,
        user.Role,
        user.DeviceId,
        user.DeviceBoundAt,
        user.ExpiredAt,
        user.IsActive,
        IsExpired: user.ExpiredAt <= DateTime.UtcNow,
        user.CreatedAt,
        user.UpdatedAt);
}
