using Microsoft.Extensions.Options;
using MyApi.Auth;

namespace MyApi.Features.Users;

/// <summary>
/// Tạo sẵn dữ liệu để test ngay sau khi init project — CHỈ chạy ở môi trường
/// Development (đăng ký có điều kiện trong Program.cs).
///
/// <para>
/// Nhắc lại kiến trúc: tài khoản ADMIN nằm ở cấu hình (AdminAuth), không nằm
/// trong collection "users", nên seeder này KHÔNG tạo admin — admin đã có sẵn
/// mỗi lần chạy. Việc của seeder là bảo đảm có một USER demo trong Mongo để đăng
/// nhập thử extension mà không phải vào Admin FE tạo tay.
/// </para>
///
/// <para>
/// Idempotent: đã có user demo thì bỏ qua. Chạy lại mỗi lần khởi động vô hại.
/// </para>
/// </summary>
public class DevDataSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<AdminAuthSettings> adminAuth,
    ILogger<DevDataSeeder> logger) : IHostedService
{
    public const string DemoEmail = "demo@example.com";
    public const string DemoPassword = "Demo@123456";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // IHostedService là singleton còn IUserService là scoped -> phải tự mở scope.
        using var scope = scopeFactory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();

        try
        {
            var existing = await users.FindByEmailAsync(DemoEmail, cancellationToken);
            if (existing is null)
            {
                await users.CreateAsync(
                    new CreateUserRequest
                    {
                        Email = DemoEmail,
                        Password = DemoPassword,
                        // Hạn xa để khỏi phải sờ lại. Đây là dữ liệu dev, không phải thật.
                        ExpiredAt = new DateTimeOffset(2099, 12, 31, 23, 59, 59, TimeSpan.Zero),
                    },
                    cancellationToken);

                logger.LogInformation("DevDataSeeder: đã tạo user demo {Email}.", DemoEmail);
            }
        }
        catch (DuplicateEmailException)
        {
            // Hai instance khởi động sát nhau cùng seed — không sao, đã có là đạt.
        }
        catch (Exception exception)
        {
            // Seed hỏng KHÔNG được làm sập app: nó chỉ là tiện ích dev.
            logger.LogWarning(exception, "DevDataSeeder: bỏ qua vì seed thất bại.");
            return;
        }

        // In rõ mọi lối vào để init xong là đăng nhập thử được ngay.
        logger.LogInformation(
            "\n╭─ Tài khoản có sẵn (Development) ──────────────────────────────\n" +
            "│  Admin FE  (config, không nằm trong DB):\n" +
            "│     email    : {AdminEmail}\n" +
            "│     mật khẩu : Admin@123456   (đổi: dotnet run -- hash-password \"...\")\n" +
            "│  User/Extension (seed trong Mongo):\n" +
            "│     email    : {DemoEmail}\n" +
            "│     mật khẩu : {DemoPassword}\n" +
            "╰───────────────────────────────────────────────────────────────",
            adminAuth.Value.Email,
            DemoEmail,
            DemoPassword);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
