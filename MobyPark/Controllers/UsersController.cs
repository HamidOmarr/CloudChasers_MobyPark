using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.User.Request;
using MobyPark.Models;
using MobyPark.Models.DTOs.User;
using MobyPark.Models.Responses.User;
using MobyPark.Services;
using MobyPark.Services.Services;
using MobyPark.Services.Results.User;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly UserService _userService;
    private readonly SessionService _sessionService;

    public UsersController(ServiceStack services) : base(services)
    {
        _userService = services.Users;
        _sessionService = services.Sessions;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _userService.CreateUserAsync(dto);

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
                    Token = _sessionService.CreateSession(s.User)
                }),
            RegisterResult.UsernameTaken => Conflict(new { error = "Username already taken" }),
            RegisterResult.InvalidData e => BadRequest(new { error = e.Message }),
            RegisterResult.Error e => StatusCode(500, new { error = e.Message }),
            _ => StatusCode(500, new { error = "Unknown result" })
        };
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _userService.LoginAsync(dto);

        return result switch
        {
            LoginResult.Success s => Ok(s.Response),
            LoginResult.InvalidCredentials or LoginResult.Error => Unauthorized(new { error = "Invalid credentials" }),
            _ => StatusCode(500, new { error = "Unexpected error" })
        };
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await GetCurrentUserAsync();

        var result = await _userService.UpdateUserProfileAsync(user, dto);

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
    public async Task<IActionResult> GetProfile()
    {
        var user = await GetCurrentUserAsync();

        return Ok( new UserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Birthday = user.Birthday
        });
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _userService.GetUserById(id);
        if (user is null) return NotFound();

        return Ok(new { user.Id, user.Username, user.FirstName, user.LastName });
    }

    [Authorize(Policy = "CanReadUsers")]
    [HttpGet("admin/users/{id:int}")]
    public async Task<IActionResult> GetUserAdmin(int id)
    {
        var user = await _userService.GetUserById(id);
        if (user is null) return NotFound();

        return Ok(new AdminUserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Birthday = user.Birthday,
            Role = ((UserRole)user.RoleId).ToString(),
            CreatedAt = user.CreatedAt
        });
    }

    [Authorize]
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        return NoContent();
    }
}
