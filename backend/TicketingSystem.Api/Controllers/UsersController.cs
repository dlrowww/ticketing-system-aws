using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Users;
using TicketingSystem.Api.Services.Users.Admin;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<Paged<UserListItemDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] byte? role = null,
        [FromQuery] byte? category = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = "userId",
        [FromQuery] string? sortDir = "asc",
        CancellationToken ct = default)
    {
        var asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var result = await _users.GetUsersAsync(
            new PageRequest { Page = page, Size = pageSize, Sort = $"{(asc ? "" : "-")}{(sortBy ?? "userId")}" },
            search,
            role,
            category,
            isActive,
            ct);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDetailsDto>> GetById([FromRoute] int id, CancellationToken ct)
    {
        var dto = await _users.GetByIdAsync(id, ct);
        if (dto is null)
        {
            throw new AppException(ErrorCodes.UserNotFound, "User not found", System.Net.HttpStatusCode.NotFound);
        }
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<UserDetailsDto>> Create([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        var created = await _users.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.UserId }, created);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<UserDetailsDto>> Update([FromRoute] int id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var updated = await _users.UpdateAsync(id, req, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _users.DeleteAsync(id, ct);
        return NoContent();
    }
}
