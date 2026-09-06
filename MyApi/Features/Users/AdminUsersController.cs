using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Auth;

namespace MyApi.Features.Users;

[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Authorize(AuthPolicies.AdminOnly)]
public class AdminUsersController(IUserService userService) : ControllerBase
{
    /// <summary>GET /api/admin/users</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>GET /api/admin/users/{id}</summary>
    [HttpGet("{id}", Name = nameof(GetById))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var user = await userService.GetByIdAsync(id, cancellationToken);

        // Pattern "is null" đọc tự nhiên hơn "== null" và không bị override toán tử
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// POST /api/admin/users — tạo user mới với <c>DeviceId = null</c>.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await userService.CreateAsync(request, cancellationToken);

            // Trả 201 kèm header Location trỏ tới GET /api/admin/users/{id}
            return CreatedAtRoute(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateEmailException exception)
        {
            return Problem(
                title: "Email đã tồn tại",
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>PUT /api/admin/users/{id}</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Update(
        string id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await userService.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (DuplicateEmailException exception)
        {
            return Problem(
                title: "Email đã tồn tại",
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>
    /// POST /api/admin/users/{id}/reset-device — gỡ thiết bị đang gắn.
    /// <para>
    /// Dùng khi user đổi máy. Lần đăng nhập kế tiếp (ở bất kỳ máy nào) sẽ gắn thiết bị mới.
    /// Là POST chứ không phải PUT vì đây là một HÀNH ĐỘNG, không phải thay thế tài nguyên.
    /// </para>
    /// </summary>
    [HttpPost("{id}/reset-device")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetDevice(string id, CancellationToken cancellationToken)
    {
        var reset = await userService.ResetDeviceAsync(id, cancellationToken);
        return reset ? NoContent() : NotFound();
    }

    /// <summary>DELETE /api/admin/users/{id}</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await userService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
