using System.ComponentModel.DataAnnotations;

namespace MyApi.Auth;

/// <summary>Body của POST /api/admin/auth/login</summary>
public record AdminLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Kết quả đăng nhập. Không có refreshToken — hết hạn thì đăng nhập lại.
/// </summary>
/// <param name="AccessToken">Gửi kèm mọi request sau đó: <c>Authorization: Bearer &lt;token&gt;</c></param>
/// <param name="ExpiresAt">Mốc hết hạn (UTC). FE dùng để tự đăng xuất trước khi API trả 401.</param>
public record AdminLoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string Email);

/// <summary>
/// Kết quả GET /api/admin/auth/me — FE gọi lúc mở app để biết token còn dùng được không,
/// và để biết phiên còn sống tới bao giờ.
/// </summary>
/// <param name="TokenIssuedAt">Mốc phát token (UTC).</param>
/// <param name="TokenExpiresAt">
/// Mốc hết hạn (UTC), đọc từ claim "exp" trong chính token gửi lên.
/// Đây mới là nguồn đáng tin — giá trị FE lưu trong localStorage có thể bị sửa.
/// </param>
public record AdminProfileResponse(
    string Email,
    string Role,
    DateTime? TokenIssuedAt,
    DateTime? TokenExpiresAt);
