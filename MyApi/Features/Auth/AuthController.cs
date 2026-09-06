using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using MyApi.Auth;
using MyApi.Features.Users;

namespace MyApi.Features.Auth;

/// <summary>
/// Đăng nhập cho User FE (extension DOM Modifier).
/// <para>
/// Tách hẳn khỏi <c>/api/admin/auth</c>: khác đường dẫn, khác nguồn tài khoản
/// (Mongo vs cấu hình), khác khóa ký, khác audience. Token của bên này không
/// dùng được ở bên kia.
/// </para>
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    IUserAuthService userAuthService,
    UserTokenIssuer tokenIssuer) : ControllerBase
{
    /// <summary>
    /// POST /api/auth/login — đăng nhập và gắn thiết bị.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserLoginResponse>> Login(
        [FromBody] UserLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAuthService.LoginAsync(
            request.Email,
            request.Password,
            request.DeviceId,
            cancellationToken);

        if (!result.IsValid)
        {
            return SessionProblem(result.Status);
        }

        var (token, expiresAt) = tokenIssuer.Issue(result.User!, result.DeviceId!);

        return Ok(new UserLoginResponse(
            token,
            expiresAt,
            result.DeviceId!,
            ToSessionResponse(result.User!)));
    }

    /// <summary>
    /// GET /api/auth/me — client gọi định kỳ để biết phiên còn dùng được không.
    /// <para>
    /// Luôn đọc trạng thái mới nhất từ DB. Token còn hạn KHÔNG có nghĩa là tài khoản
    /// còn hạn: Admin có thể vừa khóa tài khoản, rút ngắn ExpiredAt, hoặc gỡ thiết bị.
    /// </para>
    /// </summary>
    [HttpGet("me")]
    [Authorize(AuthPolicies.UserOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserSessionResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var deviceId = User.FindFirst(AuthClaims.DeviceId)?.Value;

        if (userId is null || deviceId is null)
        {
            // Token hợp lệ nhưng thiếu claim -> token cũ từ phiên bản trước.
            return Unauthorized();
        }

        var result = await userAuthService.ValidateSessionAsync(userId, deviceId, cancellationToken);

        return result.IsValid
            ? Ok(ToSessionResponse(result.User!))
            : SessionProblem(result.Status);
    }

    private static UserSessionResponse ToSessionResponse(User user) => new(
        user.Id!,
        user.Email,
        user.Role,
        user.ExpiredAt,
        user.IsActive,
        user.DeviceBoundAt);

    /// <summary>
    /// Dịch lý do từ chối sang mã HTTP + mã lỗi máy đọc được.
    /// <para>
    /// Client cần phân biệt được: "sai mật khẩu" thì cho nhập lại, còn
    /// "tài khoản hết hạn" hay "máy khác đang giữ" thì phải xóa phiên đang lưu.
    /// </para>
    /// </summary>
    private ObjectResult SessionProblem(UserSessionStatus status)
    {
        var (statusCode, title, detail) = status switch
        {
            UserSessionStatus.InvalidCredentials => (
                StatusCodes.Status401Unauthorized,
                "Đăng nhập thất bại",
                "Email hoặc mật khẩu không đúng."),

            UserSessionStatus.UserNotFound => (
                StatusCodes.Status401Unauthorized,
                "Tài khoản không tồn tại",
                "Tài khoản này đã bị xóa."),

            UserSessionStatus.AccountInactive => (
                StatusCodes.Status403Forbidden,
                "Tài khoản đã bị khóa",
                "Tài khoản đang bị khóa. Liên hệ quản trị viên để mở lại."),

            UserSessionStatus.AccountExpired => (
                StatusCodes.Status403Forbidden,
                "Tài khoản đã hết hạn",
                "Thời hạn sử dụng của tài khoản đã kết thúc."),

            UserSessionStatus.DeviceMismatch => (
                StatusCodes.Status409Conflict,
                "Tài khoản đang dùng ở thiết bị khác",
                "Mỗi tài khoản chỉ dùng được trên một thiết bị. " +
                "Liên hệ quản trị viên để gỡ thiết bị cũ."),

            _ => (
                StatusCodes.Status401Unauthorized,
                "Không truy cập được",
                "Phiên đăng nhập không hợp lệ."),
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
        };

        // Trường phụ ngoài chuẩn RFC 7807 — client so sánh chuỗi này thay vì
        // so nội dung thông báo (thông báo đổi lúc nào cũng được, mã thì không).
        problem.Extensions["code"] = UserSessionErrorCodes.From(status);

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" },
        };
    }
}
