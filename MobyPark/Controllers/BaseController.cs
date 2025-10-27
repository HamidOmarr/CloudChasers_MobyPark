using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.User;

namespace MobyPark.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly IUserService UserService;

    protected BaseController(IUserService users)
    {
        UserService = users;
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
        var userResult = await UserService.GetUserById(userId);

        if (userResult is not GetUserResult.Success success)
            throw new UnauthorizedAccessException("Authenticated user record not found.");

        return success.User;
    }
}