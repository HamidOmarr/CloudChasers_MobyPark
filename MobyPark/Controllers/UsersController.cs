using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.User.Request;
using MobyPark.DTOs.User.Response;
using MobyPark.Services;
using MobyPark.Services.Results.User;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly SessionService _sessionService;

    public UsersController(UserService users, SessionService sessions) : base(users)
    {
        _sessionService = sessions;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await UserService.CreateUserAsync(dto);

        return result switch
        {
            RegisterResult.Success s => CreatedAtAction(
                nameof(GetUser),
                new { id = s.User.Id },
                new AuthDto
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
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await UserService.LoginAsync(dto);

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
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        var result = await UserService.UpdateUserProfileAsync(user, dto);

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

        return Ok( new UserProfileDto
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
        var result = await UserService.GetUserById(id);
        if (result is not GetUserResult.Success success)
        {
            return result switch
            {
                GetUserResult.NotFound => NotFound(new { error = "User not found" }),
                _ => StatusCode(500, new { error = "Unexpected error" })
            };
        }

        var user = success.User;
        return Ok(new { user.Id, user.Username, user.FirstName, user.LastName });
    }

    [Authorize(Policy = "CanReadUsers")]
    [HttpGet("admin/users/{id:int}")]
    public async Task<IActionResult> GetUserAdmin(int id)
    {
        var result = await UserService.GetUserById(id);
        if (result is not GetUserResult.Success success)
        {
            return result switch
            {
                GetUserResult.NotFound => NotFound(new { error = "User not found" }),
                _ => StatusCode(500, new { error = "Unexpected error" })
            };
        }

        var user = success.User;
        return Ok(new AdminUserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Birthday = user.Birthday,
            Role = user.Role.Name,
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
