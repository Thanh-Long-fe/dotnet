using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using MyApi.Features.Users;

namespace MyApi.Features.Auth;

/// <summary>
/// Toàn bộ quyết định "được vào hay không" nằm ở đây. Controller chỉ dịch kết quả
/// sang mã HTTP — cố ý, để logic này test được mà không cần dựng HTTP.
/// </summary>
public class UserAuthService(
    IUserService userService,
    IPasswordHasher<User> passwordHasher,
    ILogger<UserAuthService> logger) : IUserAuthService
{
    public async Task<UserSessionResult> LoginAsync(
        string email,
        string password,
        string? clientDeviceId,
        CancellationToken cancellationToken = default)
    {
        var user = await userService.FindByEmailAsync(email, cancellationToken);

        // Vẫn băm một lần kể cả khi không tìm thấy user, để thời gian phản hồi của
        // "email không tồn tại" và "sai mật khẩu" giống nhau. Không có bước này thì
        // đo thời gian là dò ra được email nào có trong hệ thống.
        var verification = passwordHasher.VerifyHashedPassword(
            user ?? new User(),
            user?.PasswordHash ?? DummyHash,
            password);

        if (user is null || verification == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Đăng nhập user thất bại cho email {Email}.", email);
            return new UserSessionResult(UserSessionStatus.InvalidCredentials);
        }

        var accountStatus = CheckAccount(user);
        if (accountStatus != UserSessionStatus.Valid)
        {
            return new UserSessionResult(accountStatus, user);
        }

        return await BindOrMatchDeviceAsync(user, clientDeviceId, cancellationToken);
    }

    public async Task<UserSessionResult> ValidateSessionAsync(
        string userId,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        var user = await userService.FindEntityByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            // Admin đã xóa tài khoản trong lúc token còn hạn.
            return new UserSessionResult(UserSessionStatus.UserNotFound);
        }

        var accountStatus = CheckAccount(user);
        if (accountStatus != UserSessionStatus.Valid)
        {
            return new UserSessionResult(accountStatus, user);
        }

        // Admin bấm "gỡ thiết bị" -> DeviceId về null -> phiên hiện tại mất hiệu lực.
        // Máy khác chiếm suất -> DeviceId đổi -> phiên hiện tại cũng mất hiệu lực.
        if (user.DeviceId is null || user.DeviceId != deviceId)
        {
            return new UserSessionResult(UserSessionStatus.DeviceMismatch, user);
        }

        return new UserSessionResult(UserSessionStatus.Valid, user, deviceId);
    }

    /// <summary>Hạn dùng và trạng thái khóa. Chỉ đọc, không đụng tới thiết bị.</summary>
    private static UserSessionStatus CheckAccount(User user)
    {
        if (!user.IsActive) return UserSessionStatus.AccountInactive;
        if (user.ExpiredAt <= DateTime.UtcNow) return UserSessionStatus.AccountExpired;

        return UserSessionStatus.Valid;
    }

    private async Task<UserSessionResult> BindOrMatchDeviceAsync(
        User user,
        string? clientDeviceId,
        CancellationToken cancellationToken)
    {
        // Trường hợp 1: tài khoản đã gắn thiết bị.
        if (user.DeviceId is not null)
        {
            if (user.DeviceId == clientDeviceId)
            {
                return new UserSessionResult(UserSessionStatus.Valid, user, user.DeviceId);
            }

            logger.LogWarning(
                "User {UserId} đăng nhập từ thiết bị khác. Đang gắn {Bound}, gửi lên {Provided}.",
                user.Id,
                user.DeviceId,
                clientDeviceId ?? "<không có>");

            return new UserSessionResult(UserSessionStatus.DeviceMismatch, user);
        }

        // Trường hợp 2: suất còn trống -> chiếm.
        // Cố tình KHÔNG dùng lại clientDeviceId gửi lên: giá trị đó do client kiểm soát,
        // hai máy có thể cùng khai một chuỗi. Luôn sinh mới ở server.
        var newDeviceId = GenerateDeviceId();

        var bound = await userService.TryBindDeviceAsync(user.Id!, newDeviceId, cancellationToken);
        if (!bound)
        {
            // Thua cuộc đua: một máy khác vừa chiếm mất trong tích tắc.
            return new UserSessionResult(UserSessionStatus.DeviceMismatch, user);
        }

        user.DeviceId = newDeviceId;
        user.DeviceBoundAt = DateTime.UtcNow;

        logger.LogInformation("Đã gắn thiết bị mới cho user {UserId}.", user.Id);

        return new UserSessionResult(UserSessionStatus.Valid, user, newDeviceId);
    }

    /// <summary>
    /// 32 byte ngẫu nhiên mã hóa Base64Url. Sinh ở SERVER, không phải client —
    /// giá trị do client chọn thì hai người chỉ cần hẹn nhau dùng chung một chuỗi.
    /// </summary>
    private static string GenerateDeviceId() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// Hash giả để so khi không tìm thấy user. Phải là chuỗi hợp lệ, nếu không
    /// VerifyHashedPassword ném exception và lại thoát sớm — đúng cái ta đang tránh.
    /// </summary>
    private static readonly string DummyHash =
        new PasswordHasher<User>().HashPassword(new User(), "khong-bao-gio-khop");
}
