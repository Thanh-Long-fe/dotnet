using MyApi.Auth;

namespace MyApi.Features.Auth;

/// <summary>
/// Ánh xạ section "UserAuth". Tách hẳn khỏi "AdminAuth": khác khóa ký, khác audience.
/// <para>
/// Khác audience là thứ ngăn token admin dùng được ở endpoint user và ngược lại.
/// Nếu dùng chung audience thì hai hệ danh tính thành một, và toàn bộ việc
/// tách scheme ở trên chỉ còn là hình thức.
/// </para>
/// </summary>
public class UserAuthSettings
{
    public const string SectionName = "UserAuth";

    public JwtSettings Jwt { get; set; } = new();
}
