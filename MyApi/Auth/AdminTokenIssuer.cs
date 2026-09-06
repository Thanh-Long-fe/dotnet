using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MyApi.Auth;

/// <summary>
/// Ký access token cho Admin.
/// <para>
/// Dùng <see cref="JsonWebTokenHandler"/> (API mới) thay cho
/// <c>JwtSecurityTokenHandler</c> (API cũ) — nhanh hơn và không tự ý đổi tên claim.
/// </para>
/// </summary>
public class AdminTokenIssuer(IOptions<AdminAuthSettings> settings)
{
    private readonly AdminAuthSettings _settings = settings.Value;

    public AdminLoginResponse Issue()
    {
        var jwt = _settings.Jwt;
        var now = DateTime.UtcNow;
        var expiresAt = now.AddHours(jwt.ExpiresInHours);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            // NotBefore/Expires nằm TRONG token và được ký -> client sửa là chữ ký hỏng.
            NotBefore = now,
            Expires = expiresAt,
            IssuedAt = now,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, _settings.Email),
                new Claim(JwtRegisteredClaimNames.Email, _settings.Email),
                new Claim(AuthClaims.Role, UserRoles.Admin),
                // jti: mỗi token một id riêng. Chưa dùng tới, nhưng có sẵn thì
                // sau này muốn làm danh sách thu hồi token là có cái để lưu.
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new AdminLoginResponse(token, expiresAt, _settings.Email);
    }
}
