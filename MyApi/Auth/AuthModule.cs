using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace MyApi.Auth;

/// <summary>Gom việc đăng ký xác thực/phân quyền vào một extension method.</summary>
public static class AuthModule
{
    public static IServiceCollection AddAdminJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // ValidateOnStart: app CHẾT NGAY lúc khởi động nếu cấu hình thiếu.
        // Quan trọng — thiếu mà không chặn thì lỗi chỉ lộ ra lúc có người đăng nhập,
        // và khóa ký rỗng nghĩa là ai cũng tự phát được token admin.
        services.AddOptions<AdminAuthSettings>()
            .Bind(configuration.GetSection(AdminAuthSettings.SectionName))
            .Validate(
                s => !string.IsNullOrWhiteSpace(s.Email),
                $"Thiếu '{AdminAuthSettings.SectionName}:Email'.")
            .Validate(
                s => !string.IsNullOrWhiteSpace(s.PasswordHash),
                $"Thiếu '{AdminAuthSettings.SectionName}:PasswordHash'. " +
                "Sinh bằng: dotnet run --project MyApi -- hash-password \"mat-khau\"")
            .Validate(
                s => Encoding.UTF8.GetByteCount(s.Jwt.Key) >= 32,
                $"'{AdminAuthSettings.SectionName}:Jwt:Key' phải dài tối thiểu 32 byte (HMAC-SHA256).")
            .Validate(
                s => s.Jwt.ExpiresInHours > 0,
                $"'{AdminAuthSettings.SectionName}:Jwt:ExpiresInHours' phải lớn hơn 0.")
            .ValidateOnStart();

        var jwt = configuration
            .GetSection(AdminAuthSettings.SectionName)
            .Get<AdminAuthSettings>()?.Jwt ?? new JwtSettings();

        services.AddAuthentication(AuthSchemes.AdminJwt)
            .AddJwtBearer(AuthSchemes.AdminJwt, options =>
            {
                // Tắt bảng ánh xạ claim đời cũ: giữ nguyên tên claim như trong token
                // ("role" vẫn là "role", không bị đổi thành URI dài của WS-Federation).
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),

                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,

                    ValidateLifetime = true,
                    // Mặc định .NET cho lệch 5 phút để bù đồng hồ lệch giữa các máy.
                    // Ở đây API tự ký và tự kiểm nên không cần dung sai.
                    ClockSkew = TimeSpan.Zero,

                    NameClaimType = "sub",
                    RoleClaimType = AuthClaims.Role,
                };

                // Token hết hạn -> thêm header để FE phân biệt với "token sai",
                // và tự đăng xuất thay vì bắt người dùng đoán.
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

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.AdminOnly, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.AdminJwt)
                .RequireAuthenticatedUser()
                .RequireRole(UserRoles.Admin));
        });

        services.AddSingleton<IPasswordHasher<AdminAccount>, PasswordHasher<AdminAccount>>();
        services.AddSingleton<AdminTokenIssuer>();

        return services;
    }

    /// <summary>
    /// Cho phép Admin FE (chạy ở origin khác) gọi API.
    /// <para>
    /// Liệt kê origin cụ thể, KHÔNG dùng <c>AllowAnyOrigin</c>: CORS là thứ duy nhất
    /// ngăn một trang web bất kỳ đọc được response API bằng token của admin.
    /// </para>
    /// </summary>
    public const string AdminFeCorsPolicy = "AdminFe";

    public static IServiceCollection AddAdminFeCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(AdminFeCorsPolicy, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                // Để FE đọc được header đánh dấu token hết hạn.
                .WithExposedHeaders("X-Token-Expired"));
        });

        return services;
    }
}
