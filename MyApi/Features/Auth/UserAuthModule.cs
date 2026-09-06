using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MyApi.Auth;

namespace MyApi.Features.Auth;

/// <summary>Đăng ký phần đăng nhập của User FE.</summary>
public static class UserAuthModule
{
    public static IServiceCollection AddUserAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<UserAuthSettings>()
            .Bind(configuration.GetSection(UserAuthSettings.SectionName))
            .Validate(
                s => Encoding.UTF8.GetByteCount(s.Jwt.Key) >= 32,
                $"'{UserAuthSettings.SectionName}:Jwt:Key' phải dài tối thiểu 32 byte (HMAC-SHA256).")
            .Validate(
                s => !string.IsNullOrWhiteSpace(s.Jwt.Audience),
                $"'{UserAuthSettings.SectionName}:Jwt:Audience' không được để trống — " +
                "đây là thứ ngăn token admin dùng được ở endpoint user.")
            .Validate(
                s => s.Jwt.ExpiresInHours > 0,
                $"'{UserAuthSettings.SectionName}:Jwt:ExpiresInHours' phải lớn hơn 0.")
            .ValidateOnStart();

        var jwt = configuration
            .GetSection(UserAuthSettings.SectionName)
            .Get<UserAuthSettings>()?.Jwt ?? new JwtSettings();

        services.AddAuthentication()
            .AddJwtBearer(AuthSchemes.UserJwt, options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),

                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,

                    // Audience riêng: token admin có chữ ký hợp lệ theo khóa của nó
                    // nhưng audience khác nên vẫn bị từ chối ở đây, và ngược lại.
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    NameClaimType = "sub",
                    RoleClaimType = AuthClaims.Role,
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Append("X-Token-Expired", "true");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.UserOnly, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.UserJwt)
                .RequireAuthenticatedUser());

        services.AddSingleton<UserTokenIssuer>();
        services.AddScoped<IUserAuthService, UserAuthService>();

        return services;
    }
}
