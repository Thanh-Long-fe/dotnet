namespace MyApi.Auth;

/// <summary>
/// Ánh xạ section "AdminAuth" trong cấu hình.
/// <para>
/// Tài khoản Admin nằm ở CẤU HÌNH, không nằm trong collection "users". Lý do:
/// admin và user là hai hệ danh tính khác nhau (khác cách đăng nhập, khác vòng đời,
/// khác rủi ro). Để chung một bảng thì chỉ cần một bug đổi role là user tự lên admin.
/// </para>
/// <para>
/// Production: đặt bằng biến môi trường
/// <c>AdminAuth__Email</c>, <c>AdminAuth__PasswordHash</c>, <c>AdminAuth__Jwt__Key</c>
/// (hai dấu gạch dưới = dấu hai chấm trong cấu hình .NET).
/// </para>
/// </summary>
public class AdminAuthSettings
{
    public const string SectionName = "AdminAuth";

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu ĐÃ BĂM, không phải mật khẩu thô. Sinh bằng:
    /// <code>dotnet run --project MyApi -- hash-password "mat-khau-cua-ban"</code>
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public JwtSettings Jwt { get; set; } = new();
}

/// <summary>Tham số ký và kiểm tra access token.</summary>
public class JwtSettings
{
    /// <summary>
    /// Khóa bí mật để ký token (HMAC-SHA256). Tối thiểu 32 byte —
    /// ngắn hơn thì thư viện từ chối luôn. Ai có khóa này tự phát được token admin.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Ai phát token. Ghi vào claim "iss" và được kiểm lúc validate.</summary>
    public string Issuer { get; set; } = "MyApi";

    /// <summary>Token dành cho ai. Ghi vào claim "aud".</summary>
    public string Audience { get; set; } = "MyApiAdmin";

    /// <summary>
    /// Hạn của access token. Mặc định 24 giờ.
    /// <para>
    /// Không có refresh token nên con số này là đánh đổi trực tiếp:
    /// ngắn thì admin phải đăng nhập lại thường xuyên, dài thì token bị lộ sống lâu hơn.
    /// </para>
    /// </summary>
    public int ExpiresInHours { get; set; } = 24;
}
