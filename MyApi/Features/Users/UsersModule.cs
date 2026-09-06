using Microsoft.AspNetCore.Identity;

namespace MyApi.Features.Users;

/// <summary>
/// Thay thế cho @Module({ providers: [UserService] }) của NestJS.
/// Không có DI container con — chỉ là cách gom đăng ký để Program.cs gọn gàng.
/// </summary>
public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        // Scoped = 1 instance cho mỗi HTTP request (tương đương Scope.REQUEST của Nest)
        services.AddScoped<IUserService, UserService>();

        // PasswordHasher<T> có sẵn trong shared framework ASP.NET Core, KHÔNG cần cài package.
        // Mặc định dùng PBKDF2-HMAC-SHA256, salt ngẫu nhiên nhúng luôn trong chuỗi kết quả.
        // Singleton vì nó không giữ state, tạo mới mỗi request là lãng phí.
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        // Chạy một lần lúc app khởi động để dựng index cho collection "users".
        services.AddHostedService<UsersIndexInitializer>();

        return services;
    }
}
