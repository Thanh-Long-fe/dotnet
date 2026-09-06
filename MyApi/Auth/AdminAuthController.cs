using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MyApi.Auth;

/// <summary>
/// Đăng nhập dành riêng cho Admin. Tách hẳn khỏi (tương lai) <c>/api/auth/login</c>
/// của User FE: khác đường dẫn, khác cách chứng minh danh tính, khác nguồn tài khoản.
/// </summary>
[ApiController]
[Route("api/admin/auth")]
[Produces("application/json")]
public class AdminAuthController(
    IOptions<AdminAuthSettings> settings,
    IPasswordHasher<AdminAccount> passwordHasher,
    AdminTokenIssuer tokenIssuer,
    ILogger<AdminAuthController> logger) : ControllerBase
{
    private readonly AdminAuthSettings _settings = settings.Value;

    /// <summary>POST /api/admin/auth/login</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AdminLoginResponse> Login([FromBody] AdminLoginRequest request)
    {
        var emailMatches = string.Equals(
            request.Email.Trim(),
            _settings.Email,
            StringComparison.OrdinalIgnoreCase);

        // Luôn chạy VerifyHashedPassword kể cả khi email đã sai, để thời gian phản hồi
        // của "sai email" và "sai mật khẩu" giống nhau — không cho dò ra email admin.
        var passwordResult = passwordHasher.VerifyHashedPassword(
            AdminAccount.Instance,
            _settings.PasswordHash,
            request.Password);

        if (!emailMatches || passwordResult == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Đăng nhập admin thất bại cho email {Email}.", request.Email);

            // Thông báo chung chung, không nói "sai email" hay "sai mật khẩu".
            return Problem(
                title: "Đăng nhập thất bại",
                detail: "Email hoặc mật khẩu không đúng.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Ok(tokenIssuer.Issue());
    }

    /// <summary>
    /// GET /api/admin/auth/me — FE gọi lúc mở app để biết token trong localStorage
    /// còn dùng được không, thay vì đợi request đầu tiên trả 401.
    /// </summary>
    [HttpGet("me")]
    [Authorize(AuthPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AdminProfileResponse> Me() =>
        // Đọc từ CLAIM trong token, KHÔNG đọc từ cấu hình: /me phải mô tả danh tính
        // của chính cái token đang gửi lên. Nếu ai đó đổi AdminAuth:Email sau khi
        // token được phát, token cũ vẫn phải khai đúng nó là ai.
        Ok(new AdminProfileResponse(
            Email: User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty,
            Role: User.FindFirst(AuthClaims.Role)?.Value ?? string.Empty,
            TokenIssuedAt: ReadUnixTimeClaim(JwtRegisteredClaimNames.Iat),
            TokenExpiresAt: ReadUnixTimeClaim(JwtRegisteredClaimNames.Exp)));

    /// <summary>
    /// Các mốc thời gian chuẩn của JWT ("iat", "exp", "nbf") được ghi dạng
    /// <b>số giây kể từ 1970-01-01 UTC</b>, không phải chuỗi ISO.
    /// </summary>
    private DateTime? ReadUnixTimeClaim(string claimType) =>
        long.TryParse(User.FindFirst(claimType)?.Value, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : null;
}
