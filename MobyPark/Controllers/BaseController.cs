using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.User;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerResponse(400, "Invalid data supplied")]
[SwaggerResponse(401, "Unauthorized")]
[SwaggerResponse(500, "Unexpected internal server error")]
public abstract class BaseController : ControllerBase
{
    protected readonly IUserService _userService;

    protected BaseController(IUserService users)
    {
        _userService = users;
    }

    [SwaggerOperation("Gets the current authenticated user's ID from the JWT token.")]
    protected long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !long.TryParse(userIdClaim.Value, out long userId))
            throw new InvalidOperationException("User ID claim is missing or invalid in the token.");

        return userId;
    }


    /// <exception cref="UnauthorizedAccessException">Thrown if the authenticated user's record cannot be found.</exception>
    [SwaggerOperation("Gets the current authenticated user's full record.")]
    protected async Task<UserModel> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        var userResult = await _userService.GetUserById(userId);

        if (userResult is not GetUserResult.Success success)
            throw new UnauthorizedAccessException("Authenticated user record not found.");

        return success.User;
    }

    [SwaggerOperation("Converts a ServiceResult to an appropriate IActionResult.")]
    protected IActionResult FromServiceResult<T>(ServiceResult<T> result)
    {
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail or ServiceStatus.Conflict => Conflict(result.Error),
            ServiceStatus.Forbidden => StatusCode(403, result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }
}