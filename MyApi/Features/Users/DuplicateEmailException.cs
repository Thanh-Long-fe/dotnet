namespace MyApi.Features.Users;

/// <summary>
/// Ném ra khi email đã tồn tại. Có kiểu riêng (thay vì trả <c>null</c> hay <c>false</c>)
/// để controller phân biệt được "trùng email" với "không tìm thấy" và trả đúng 409.
/// </summary>
public class DuplicateEmailException(string email)
    : Exception($"Email '{email}' đã được sử dụng.")
{
    public string Email { get; } = email;
}
