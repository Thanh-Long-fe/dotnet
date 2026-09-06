using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MyApi.Auth;

namespace MyApi.Features.Users;

/// <summary>
/// Entity — hình dạng của document trong collection "users".
/// Dùng class (không dùng record) vì entity có state thay đổi được.
/// </summary>
public class User
{
    /// <summary>
    /// [BsonId] đánh dấu đây là khóa chính _id của Mongo.
    /// [BsonRepresentation(ObjectId)] cho phép làm việc với string trong C#
    /// nhưng lưu xuống DB dạng ObjectId — khỏi phải tự chuyển đổi qua lại.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Luôn lưu dạng chữ thường đã trim (xem <c>UserService.NormalizeEmail</c>).
    /// Chuẩn hóa TRƯỚC khi lưu thì unique index mới chặn được
    /// "A@x.com" và "a@x.com" đăng ký thành hai tài khoản.
    /// </summary>
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu đã băm. KHÔNG BAO GIỜ lộ ra ngoài qua DTO —
    /// đây là lý do <see cref="UserResponse"/> phải tách khỏi entity.
    /// </summary>
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Xem <see cref="UserRoles"/>. Admin tạo user thì luôn là "user".</summary>
    [BsonElement("role")]
    public string Role { get; set; } = UserRoles.User;

    /// <summary>
    /// Thiết bị/browser duy nhất được phép dùng tài khoản này.
    /// <para>
    /// <c>null</c> = chưa gắn thiết bị nào. Admin tạo user xong luôn là <c>null</c>:
    /// giá trị này chỉ sinh ra ở lần user ĐĂNG NHẬP THÀNH CÔNG đầu tiên, do backend
    /// tự sinh. Admin không thể (và không nên) đoán trước user sẽ ngồi máy nào.
    /// </para>
    /// <para>
    /// Là chuỗi ngẫu nhiên do server sinh, KHÔNG phải hardware id, không phải IP,
    /// không phải fingerprint.
    /// </para>
    /// </summary>
    [BsonElement("deviceId")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Thời điểm gắn thiết bị hiện tại. Dùng để hiển thị cho Admin
    /// ("gắn máy từ 12/03") và sau này để giới hạn tần suất reset.
    /// </summary>
    [BsonElement("deviceBoundAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? DeviceBoundAt { get; set; }

    /// <summary>Hạn dùng tài khoản. Luôn là UTC.</summary>
    [BsonElement("expiredAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiredAt { get; set; }

    /// <summary>Admin khóa/mở tài khoản thủ công, độc lập với <see cref="ExpiredAt"/>.</summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Điều kiện để được đăng nhập. Đặt ở entity (server-side) chứ không phải ở FE:
    /// FE chỉ dùng để ẩn/hiện nút, backend mới là nơi quyết định.
    /// <para>
    /// <c>[BsonIgnore]</c> vì đây là thuộc tính tính toán, không lưu xuống DB —
    /// lưu một cờ "hết hạn" là sai, nó sẽ cũ ngay sau khi ghi.
    /// </para>
    /// </summary>
    [BsonIgnore]
    public bool CanSignIn => IsActive && ExpiredAt > DateTime.UtcNow;
}
