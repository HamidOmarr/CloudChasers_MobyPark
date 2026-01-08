using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;
using MobyPark.Services.Results.User;

namespace MobyPark.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly IUserService _userService;

    protected BaseController(IUserService users)
    {
        _userService = users;
    }

    protected long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !long.TryParse(userIdClaim.Value, out long userId))
            throw new InvalidOperationException("User ID claim is missing or invalid in the token.");

        return userId;
    }

    protected async Task<UserModel> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        var userResult = await _userService.GetUserById(userId);

        if (userResult is not GetUserResult.Success success)
            throw new UnauthorizedAccessException("Authenticated user record not found.");

        return success.User;
    }

    protected IActionResult FromServiceResult<T>(ServiceResult<T> result)
    {
        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Data),
            ServiceStatus.NotFound => NotFound(result.Error),
            ServiceStatus.BadRequest => BadRequest(result.Error),
            ServiceStatus.Fail => Conflict(result.Error),
            ServiceStatus.Forbidden => StatusCode(403, result.Error),
            ServiceStatus.Exception => StatusCode(500, result.Error),
            _ => BadRequest("Unknown error")
        };
    }
}