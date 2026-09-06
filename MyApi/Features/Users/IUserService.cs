namespace MyApi.Features.Users;

/// <summary>
/// Hợp đồng của tầng nghiệp vụ. Controller chỉ biết interface này,
/// nhờ vậy có thể thay implementation hoặc mock khi viết test.
/// </summary>
public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <exception cref="DuplicateEmailException">Email đã tồn tại.</exception>
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <returns><c>null</c> nếu không tìm thấy user.</returns>
    /// <exception cref="DuplicateEmailException">Email mới trùng với user khác.</exception>
    Task<UserResponse?> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <returns><c>false</c> nếu không tìm thấy user.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gỡ thiết bị đang gắn (đặt <c>DeviceId = null</c>) để user đăng nhập được trên máy mới.
    /// Lần login kế tiếp — bất kể ở máy nào — sẽ chiếm chỗ và gắn thiết bị mới.
    /// </summary>
    /// <returns><c>false</c> nếu không tìm thấy user.</returns>
    Task<bool> ResetDeviceAsync(string id, CancellationToken cancellationToken = default);

    // ─── Dành cho luồng đăng nhập của User FE ───────────────────────────────
    // Ba method dưới trả về ENTITY chứ không phải DTO, vì tầng đăng nhập cần
    // PasswordHash và DeviceId — những thứ cố tình không có trong UserResponse.
    // Đây là lý do chúng nằm tách ở đây thay vì trộn với phần CRUD của Admin.

    /// <summary>Tìm theo email (tự chuẩn hóa chữ thường). <c>null</c> nếu không có.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Tìm theo id. <c>null</c> nếu id sai định dạng hoặc không tồn tại.</summary>
    Task<User?> FindEntityByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gắn thiết bị vào tài khoản, CHỈ KHI tài khoản chưa gắn thiết bị nào.
    /// <para>
    /// Điều kiện "chưa gắn" nằm trong chính câu lệnh update nên MongoDB kiểm và ghi
    /// trong một thao tác không thể xen ngang. Hai thiết bị đăng nhập cùng lúc thì
    /// đúng một cái thắng — đây là điểm mấu chốt của cả tính năng khóa thiết bị.
    /// </para>
    /// </summary>
    /// <returns><c>false</c> nếu tài khoản đã bị thiết bị khác chiếm mất.</returns>
    Task<bool> TryBindDeviceAsync(string userId, string deviceId, CancellationToken cancellationToken = default);
}
