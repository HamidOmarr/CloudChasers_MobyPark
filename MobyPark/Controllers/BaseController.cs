using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly UserService UserService;

    protected BaseController(UserService users)
    {
        UserService = users;
    }

    protected int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            throw new InvalidOperationException("User ID claim is missing or invalid in the token.");

        return userId;
    }

    protected async Task<UserModel> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        var user = await UserService.GetUserById(userId);

        if (user is null)
            throw new UnauthorizedAccessException("Authenticated user record not found.");

        return user;
    }
}