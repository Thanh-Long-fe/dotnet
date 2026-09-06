using MyApi.Features.Users;

namespace MyApi.Features.Auth;

/// <summary>Kết quả của một lần đăng nhập hoặc một lần kiểm tra phiên.</summary>
/// <param name="Status">Khác <see cref="UserSessionStatus.Valid"/> thì <paramref name="User"/> có thể null.</param>
public record UserSessionResult(UserSessionStatus Status, User? User = null, string? DeviceId = null)
{
    public bool IsValid => Status == UserSessionStatus.Valid;
}

public interface IUserAuthService
{
    /// <summary>
    /// Xác thực email/mật khẩu, kiểm tra hạn tài khoản, rồi gắn hoặc đối chiếu thiết bị.
    /// </summary>
    /// <param name="clientDeviceId">DeviceId client đang giữ, hoặc <c>null</c> nếu là máy mới.</param>
    Task<UserSessionResult> LoginAsync(
        string email,
        string password,
        string? clientDeviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra một phiên còn dùng được không, ĐỌC TRẠNG THÁI MỚI NHẤT TỪ DB.
    /// <para>
    /// Phải hỏi DB chứ không được tin token: Admin có thể khóa tài khoản, rút ngắn
    /// hạn, hoặc gỡ thiết bị sau khi token đã phát. Token vẫn còn chữ ký hợp lệ
    /// nhưng tài khoản thì không còn hợp lệ nữa.
    /// </para>
    /// </summary>
    Task<UserSessionResult> ValidateSessionAsync(
        string userId,
        string deviceId,
        CancellationToken cancellationToken = default);
}
