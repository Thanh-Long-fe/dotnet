namespace MyApi.Auth;

/// <summary>
/// Tên các authentication scheme. Mỗi scheme là một CÁCH chứng minh danh tính.
/// <para>
/// Hệ thống này cố tình có hai scheme tách rời:
/// <list type="bullet">
///   <item><b>AdminJwt</b> — access token Bearer, dành cho Admin FE. Đang dùng.</item>
///   <item><b>UserCookie</b> — cookie HttpOnly + device binding, dành cho User FE. CHƯA làm.</item>
/// </list>
/// Tách ra để sau này thêm UserCookie không đụng gì tới đường đi của Admin,
/// và một token Admin bị lộ cũng không tự nhiên trở thành phiên đăng nhập của User.
/// </para>
/// </summary>
public static class AuthSchemes
{
    public const string AdminJwt = "AdminJwt";

    /// <summary>Access token của User FE (extension). Khác audience với admin.</summary>
    public const string UserJwt = "UserJwt";
}

/// <summary>Tên policy dùng trong <c>[Authorize(...)]</c>.</summary>
public static class AuthPolicies
{
    /// <summary>Chỉ chấp nhận request mang access token admin còn hạn.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Chỉ chấp nhận access token user còn hạn.
    /// <para>
    /// Policy này CHỈ kiểm token. Tài khoản còn hạn không, có bị khóa không,
    /// thiết bị có khớp không — những thứ đó đổi được sau khi token đã phát,
    /// nên phải hỏi DB ở từng request (xem <c>UserAuthService.ValidateSessionAsync</c>).
    /// </para>
    /// </summary>
    public const string UserOnly = "UserOnly";
}

/// <summary>
/// Giá trị hợp lệ của <c>User.Role</c> và của claim "role" trong access token.
/// Dùng hằng số thay vì gõ chuỗi rải rác để đổi tên một chỗ là xong.
/// </summary>
public static class UserRoles
{
    public const string Admin = "admin";

    public const string User = "user";

    /// <summary>Dùng cho validation trong DTO.</summary>
    public const string AllowedPattern = "^(admin|user)$";
}

/// <summary>
/// Tên claim dùng trong token. Cố tình dùng tên NGẮN ("role") thay vì URI dài của
/// <c>ClaimTypes</c>: token gọn hơn và không phụ thuộc vào bảng ánh xạ claim
/// mặc định của thư viện (bảng này đã bị tắt trong AuthModule).
/// </summary>
public static class AuthClaims
{
    public const string Role = "role";

    /// <summary>
    /// Thiết bị mà token này được phát cho. Nằm TRONG token và được ký,
    /// nên client không sửa được — khỏi phải tin vào header do client gửi lên.
    /// </summary>
    public const string DeviceId = "device";
}
