using System.ComponentModel.DataAnnotations;

namespace MyApi.Features.Auth;

/// <summary>Body của POST /api/auth/login</summary>
public record UserLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// DeviceId mà client đang giữ từ lần đăng nhập trước (nếu có).
    /// <para>
    /// Client gửi lên để chứng minh "vẫn là tôi, cái máy cũ" khi access token đã hết hạn.
    /// Bỏ trống ở lần đăng nhập đầu tiên — lúc đó backend sinh mới.
    /// </para>
    /// </summary>
    public string? DeviceId { get; init; }
}

/// <summary>
/// Kết quả đăng nhập thành công.
/// </summary>
/// <param name="DeviceId">
/// Client PHẢI lưu lại và gửi kèm ở những lần đăng nhập sau, nếu không sẽ bị coi là máy mới.
/// </param>
public record UserLoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string DeviceId,
    UserSessionResponse User);

/// <summary>
/// Kết quả GET /api/auth/me — trạng thái tài khoản ĐỌC TỪ DB tại thời điểm gọi,
/// không phải thông tin đóng băng trong token.
/// </summary>
public record UserSessionResponse(
    string Id,
    string Email,
    string Role,
    DateTime ExpiredAt,
    bool IsActive,
    DateTime? DeviceBoundAt);

/// <summary>
/// Lý do một phiên bị từ chối. Client dùng để hiện đúng thông báo và quyết định
/// có nên xóa phiên đang lưu hay không.
/// </summary>
public enum UserSessionStatus
{
    Valid,

    /// <summary>Sai email hoặc mật khẩu.</summary>
    InvalidCredentials,

    /// <summary>Tài khoản không còn tồn tại (Admin đã xóa).</summary>
    UserNotFound,

    /// <summary>Admin đã khóa tài khoản.</summary>
    AccountInactive,

    /// <summary>Đã quá ExpiredAt.</summary>
    AccountExpired,

    /// <summary>Tài khoản đang gắn với một thiết bị khác.</summary>
    DeviceMismatch,
}

/// <summary>Mã lỗi trả cho client trong trường "code" của ProblemDetails.</summary>
public static class UserSessionErrorCodes
{
    public const string InvalidCredentials = "invalid_credentials";
    public const string UserNotFound = "user_not_found";
    public const string AccountInactive = "account_inactive";
    public const string AccountExpired = "account_expired";
    public const string DeviceMismatch = "device_mismatch";

    public static string From(UserSessionStatus status) => status switch
    {
        UserSessionStatus.InvalidCredentials => InvalidCredentials,
        UserSessionStatus.UserNotFound => UserNotFound,
        UserSessionStatus.AccountInactive => AccountInactive,
        UserSessionStatus.AccountExpired => AccountExpired,
        UserSessionStatus.DeviceMismatch => DeviceMismatch,
        _ => "unknown",
    };
}
