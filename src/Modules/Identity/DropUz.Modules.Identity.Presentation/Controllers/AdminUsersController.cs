using DropUz.Common.Application.Pagination;
using DropUz.Common.Presentation.Authorization;
using DropUz.Modules.Identity.Application.Users;
using DropUz.Modules.Identity.Application.Users.ChangeUserRole;
using DropUz.Modules.Identity.Application.Users.GetUsers;
using DropUz.Modules.Identity.Presentation.Contracts;
using DropUz.Modules.Identity.Presentation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DropUz.Modules.Identity.Presentation.Controllers;

[AdminAuthorize]
[ApiController]
[Route("api/identity/admin/users")]
[Produces("application/json")]
public sealed class AdminUsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        [FromQuery] string? phoneNumber,
        [FromQuery] string? role,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        CancellationToken cancellationToken)
    {
        var query = new GetUsersQuery(
            pageNumber,
            pageSize,
            search,
            phoneNumber,
            role,
            sortBy,
            sortDirection);

        var result = await sender.Send(query, cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPut("{userId:guid}/role")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeRole(
        [FromRoute] Guid userId,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeUserRoleCommand(userId, request.Role);
        var result = await sender.Send(command, cancellationToken);

        return result.ToActionResult(this);
    }
}
