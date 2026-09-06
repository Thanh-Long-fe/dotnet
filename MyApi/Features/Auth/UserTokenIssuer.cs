using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MyApi.Auth;
using MyApi.Features.Users;

namespace MyApi.Features.Auth;

/// <summary>Ký access token cho User FE.</summary>
public class UserTokenIssuer(IOptions<UserAuthSettings> settings)
{
    private readonly JwtSettings _jwt = settings.Value.Jwt;

    public (string Token, DateTime ExpiresAt) Issue(User user, string deviceId)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddHours(_jwt.ExpiresInHours);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            NotBefore = now,
            Expires = expiresAt,
            IssuedAt = now,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id!),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(AuthClaims.Role, user.Role),

                // Thiết bị được ghi vào token và được ký. Nhờ vậy mỗi request sau đó
                // backend biết token này thuộc về máy nào mà không cần tin header
                // do client tự khai — client sửa claim là chữ ký hỏng ngay.
                new Claim(AuthClaims.DeviceId, deviceId),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                SecurityAlgorithms.HmacSha256),
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
