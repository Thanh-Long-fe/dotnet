namespace MyApi.Auth;

/// <summary>
/// Kiểu đánh dấu cho <c>IPasswordHasher&lt;AdminAccount&gt;</c>.
/// <para>
/// <c>PasswordHasher&lt;T&gt;</c> không hề đụng tới đối tượng T (salt nằm sẵn trong
/// chuỗi hash), T chỉ để phân biệt các đăng ký DI khác nhau. Có kiểu riêng cho admin
/// giúp không lẫn với <c>IPasswordHasher&lt;User&gt;</c> của module Users.
/// </para>
/// </summary>
public sealed class AdminAccount
{
    public static readonly AdminAccount Instance = new();

    private AdminAccount()
    {
    }
}
