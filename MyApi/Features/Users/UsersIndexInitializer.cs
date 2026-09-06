using MongoDB.Driver;
using MyApi.Data;

namespace MyApi.Features.Users;

/// <summary>
/// Tạo index cho collection "users" lúc app khởi động.
/// <para>
/// MongoDB KHÔNG tự tạo index (trừ <c>_id</c>). Không có unique index trên email thì
/// đoạn "check email tồn tại rồi mới insert" trong service chỉ là lời khuyên lịch sự:
/// hai request đến cùng lúc sẽ cùng thấy "chưa tồn tại" và tạo ra hai tài khoản trùng.
/// Chỉ database mới chặn được, vì chỉ nó thấy toàn bộ ghi.
/// </para>
/// <para>
/// <c>CreateOne</c> là idempotent — chạy lại với cùng đặc tả thì không làm gì thêm,
/// nên gọi mỗi lần khởi động là an toàn.
/// </para>
/// </summary>
public class UsersIndexInitializer(MongoContext context, ILogger<UsersIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var users = context.GetCollection<User>(UserService.CollectionName);

        var indexes = new[]
        {
            // Vừa chặn trùng email, vừa tăng tốc tra cứu lúc login (login luôn tìm theo email).
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_users_email" }),

            // Cho màn hình Admin lọc "user đang gắn thiết bị nào".
            // Sparse: chỉ đánh index document có deviceId != null — phần lớn user mới tạo là null.
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.DeviceId),
                new CreateIndexOptions { Sparse = true, Name = "ix_users_deviceId" }),
        };

        try
        {
            await users.Indexes.CreateManyAsync(indexes, cancellationToken);
        }
        catch (MongoCommandException exception)
        {
            // Hay gặp nhất: DB đang có sẵn email trùng nên không dựng được unique index.
            // Cố tình để app chết thay vì chạy tiếp mà không có ràng buộc.
            logger.LogError(
                exception,
                "Không tạo được index cho collection '{Collection}'. " +
                "Nếu lỗi là duplicate key, hãy dọn email trùng trong DB rồi khởi động lại.",
                UserService.CollectionName);

            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
