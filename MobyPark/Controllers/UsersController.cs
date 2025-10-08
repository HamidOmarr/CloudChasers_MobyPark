using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Requests.User;
using MobyPark.Services;
using MobyPark.Services.Results.User;
using MobyPark.Services.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly UserService _userService;

    public UsersController(ServiceStack services) : base(services.Sessions)
    {
        _userService = services.Users;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _userService.CreateUserAsync(request);
        return result switch
        {
            RegisterResult.Success s => CreatedAtAction(
                nameof(GetUser),
                new { id = s.User.Id },
                new AuthResponse
                {
                    UserId = s.User.Id,
                    Username = s.User.Username,
                    Email = s.User.Email,
                    Token = SessionService.CreateSession(s.User)
                }),
            RegisterResult.UsernameTaken => Conflict(new { error = "Username already taken" }),
            RegisterResult.InvalidData e => BadRequest(new { error = e.Message }),
            RegisterResult.Error e => StatusCode(500, new { error = e.Message }),
            _ => StatusCode(500, new { error = "Unknown result" })
        };
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.LoginAsync(request);

        return result switch
        {
            LoginResult.Success s => Ok(s.Response),
            LoginResult.InvalidCredentials => Unauthorized(new { error = "Invalid credentials" }),
            LoginResult.Error e => BadRequest(new { error = e.Message }),
            _ => StatusCode(500, new { error = "Unexpected error" })
        };
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = GetCurrentUser();

        var result = await _userService.UpdateUserProfileAsync(user, request);

        return result switch
        {
            UpdateProfileResult.Success s => Ok(new { message = "User updated successfully", user = s.User }),
            UpdateProfileResult.UsernameTaken => Conflict(new { error = "Username already taken" }),
            UpdateProfileResult.InvalidData e => BadRequest(new { error = e.Message }),
            UpdateProfileResult.Error e => StatusCode(500, new { error = e.Message }),
            _ => StatusCode(500, new { error = "Unexpected error" })
        };
    }

    [Authorize]
    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        var user = GetCurrentUser();
        return Ok( new UserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            BirthYear = user.BirthYear
        });
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _userService.GetUserById(id);
        if (user is null || user.Active == false) return NotFound();

        return Ok(new { user.Id, user.Username, user.Name });
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet("admin/users/{id:int}")]
    public async Task<IActionResult> GetUserAdmin(int id)
    {
        var user = await _userService.GetUserById(id);
        if (user is null || user.Active == false) return NotFound();

        return Ok(new AdminUserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            BirthYear = user.BirthYear,
            Role = user.Role,
            Active = user.Active,
            CreatedAt = user.CreatedAt
        });
    }

    [Authorize]
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var token))
            return BadRequest(new { error = "Invalid session token" });

        SessionService.RemoveSession(token);
        return Ok(new { message = "User logged out" });
    }
}
