using System.ComponentModel.DataAnnotations;
using MyApi.Auth;

namespace MyApi.Features.Users;

// DTO = hợp đồng với thế giới bên ngoài. Không bao giờ trả entity thẳng ra API.
// Các attribute [Required], [EmailAddress]... thay cho class-validator của NestJS.
// Nhờ [ApiController] trên controller, chúng được kiểm tra TỰ ĐỘNG trước khi
// vào action — sai thì trả 400 kèm ProblemDetails, không cần viết dòng nào.

/// <summary>Body của POST /api/admin/users</summary>
public record CreateUserRequest : IValidatableObject
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Mật khẩu thô. Chỉ sống trong DTO này — service băm ngay rồi vứt,
    /// không lưu và không log ra nguyên văn.
    /// </summary>
    [Required]
    [MinLength(6)]
    [MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Hạn dùng tài khoản. Dùng <see cref="DateTimeOffset"/> chứ không phải
    /// <c>DateTime</c>: kiểu này BẮT client nói rõ múi giờ ("...Z" hoặc "+07:00"),
    /// nên "2026-12-31T23:59:59Z" và "2027-01-01T06:59:59+07:00" quy về đúng một mốc.
    /// Nếu dùng DateTime, chuỗi không có hậu tố múi giờ sẽ bị hiểu theo giờ máy chủ —
    /// chạy Docker (UTC) và chạy máy dev (UTC+7) sẽ ra hai kết quả khác nhau.
    /// </summary>
    [Required]
    public DateTimeOffset ExpiredAt { get; init; }

    /// <summary>
    /// Kiểm tra vượt quá khả năng của attribute. <c>[ApiController]</c> tự gọi hàm này
    /// và trả 400 nếu có lỗi — không cần viết gì trong controller.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExpiredAt <= DateTimeOffset.UtcNow)
        {
            yield return new ValidationResult(
                "ExpiredAt phải là một thời điểm trong tương lai.",
                [nameof(ExpiredAt)]);
        }
    }
}

/// <summary>
/// Body của PUT /api/admin/users/{id}.
/// <para>
/// Cố tình KHÔNG có Password (đổi mật khẩu cần luồng riêng) và KHÔNG có DeviceId
/// (gắn thiết bị là việc của backend lúc user login; gỡ thiết bị dùng endpoint reset-device).
/// </para>
/// </summary>
public record UpdateUserRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [RegularExpression(UserRoles.AllowedPattern, ErrorMessage = "Role chỉ nhận 'admin' hoặc 'user'.")]
    public string Role { get; init; } = UserRoles.User;

    [Required]
    public DateTimeOffset ExpiredAt { get; init; }

    // Lưu ý: ở đây KHÔNG bắt ExpiredAt phải ở tương lai như lúc tạo mới —
    // Admin cần quyền chỉnh lùi hạn để cắt truy cập ngay lập tức.
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Dữ liệu trả về cho Admin FE. Không có PasswordHash — tách DTO khỏi entity
/// để field bí mật không vô tình lọt ra API khi ai đó thêm field mới vào <see cref="User"/>.
/// </summary>
public record UserResponse(
    string Id,
    string Email,
    string Role,
    string? DeviceId,
    DateTime? DeviceBoundAt,
    DateTime ExpiredAt,
    bool IsActive,
    bool IsExpired,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
