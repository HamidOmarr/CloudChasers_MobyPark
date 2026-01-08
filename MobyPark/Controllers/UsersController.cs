using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.User.Request;
using MobyPark.DTOs.User.Response;
using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Tokens;
using MobyPark.Services.Results.User;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly TokenService _tokenService;

    public UsersController(IUserService users, TokenService tokens) : base(users)
    {
        _tokenService = tokens;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _userService.CreateUserAsync(dto);

        return result switch
        {
            RegisterResult.Success success => HandleRegistrationSuccess(success.User),
            RegisterResult.UsernameTaken => Conflict(new { error = "Username already taken" }),
            RegisterResult.InvalidData invalid => BadRequest(new { error = invalid.Message }),
            RegisterResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unknown result" })
        };

        IActionResult HandleRegistrationSuccess(UserModel user)
        {
            var tokenResult = _tokenService.CreateToken(user);

            if (tokenResult is not CreateJwtResult.Success tokenSuccess)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Server configuration error generating token." });
            }

            return CreatedAtAction(
                nameof(GetUser),
                new { id = user.Id },
                new AuthDto
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Token = tokenSuccess.JwtToken
                });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _userService.Login(dto);

        return result switch
        {
            LoginResult.Success s => Ok(s.Response),
            LoginResult.InvalidCredentials or LoginResult.Error => Unauthorized(new { error = "Invalid credentials" }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unexpected error" })
        };
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        var result = await _userService.UpdateUserProfile(user.Id, dto);

        return result switch
        {
            UpdateUserResult.Success success => Ok(new { message = "User updated successfully", user = success.User }),
            UpdateUserResult.NoChangesMade => Ok(new { message = "No changes made to the user profile" }),
            UpdateUserResult.NotFound => NotFound(new { error = "User not found" }),
            UpdateUserResult.UsernameTaken => Conflict(new { error = "Username already taken" }),
            UpdateUserResult.InvalidData invalid => BadRequest(new { error = invalid.Message }),
            UpdateUserResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unexpected error" })
        };
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await GetCurrentUserAsync();

        return Ok(new UserProfileDto
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

    [Authorize(Policy = "CanReadUsers")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await _userService.GetUserById(id);
        if (result is not GetUserResult.Success success)
        {
            return result switch
            {
                GetUserResult.NotFound => NotFound(new { error = "User not found" }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unexpected error" })
            };
        }

        var user = success.User;
        return Ok(new { user.Id, user.Username, user.FirstName, user.LastName });
    }

    [Authorize(Policy = "CanReadUsers")]
    [HttpGet("admin/users/{id:int}")]
    public async Task<IActionResult> GetUserAdmin(int id)
    {
        var result = await _userService.GetUserById(id);
        if (result is not GetUserResult.Success success)
        {
            return result switch
            {
                GetUserResult.NotFound => NotFound(new { error = "User not found" }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unexpected error" })
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

    [Authorize(Policy = "CanManageUsers")]
    [HttpPut("admin/users/{id:long}/identity")]
    public async Task<IActionResult> UpdateUserIdentity(long id, [FromBody] UpdateUserIdentityDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _userService.UpdateUserIdentity(id, dto);

        return result switch
        {
            UpdateUserResult.Success success => Ok(new
            {
                message = "User identity updated successfully",
                user = new AdminUserProfileDto
                {
                    Id = success.User.Id,
                    Username = success.User.Username,
                    FirstName = success.User.FirstName,
                    LastName = success.User.LastName,
                    Email = success.User.Email,
                    Phone = success.User.Phone,
                    Birthday = success.User.Birthday,
                    Role = success.User.Role.Name,
                    CreatedAt = success.User.CreatedAt
                }
            }),
            UpdateUserResult.NoChangesMade => Ok(new { message = "No identity changes applied to the user." }),
            UpdateUserResult.NotFound => NotFound(new { error = "User not found" }),
            UpdateUserResult.InvalidData invalid => BadRequest(new { error = invalid.Message }),
            UpdateUserResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unexpected error while updating user identity." })
        };
    }

    [Authorize(Policy = "CanManageUsers")]
    [HttpPut("admin/users/{id:long}/role")]
    public async Task<IActionResult> UpdateUserRole(long id, [FromBody] UpdateUserRoleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _userService.UpdateUserRole(id, dto);
        return result switch
        {
            UpdateUserResult.Success success => Ok(new
            {
                message = "User role updated successfully",
                user = new AdminUserProfileDto
                {
                    Id = success.User.Id,
                    Username = success.User.Username,
                    FirstName = success.User.FirstName,
                    LastName = success.User.LastName,
                    Email = success.User.Email,
                    Phone = success.User.Phone,
                    Birthday = success.User.Birthday,
                    Role = success.User.Role.Name,
                    CreatedAt = success.User.CreatedAt
                }
            }),
            UpdateUserResult.NoChangesMade => Ok(new { message = "User already has the specified role." }),
            UpdateUserResult.NotFound => NotFound(new { error = "User not found" }),
            UpdateUserResult.InvalidData invalid => BadRequest(new { error = invalid.Message }),
            UpdateUserResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unexpected error while updating user role." })
        };
    }

    [Authorize(Policy = "CanManageUsers")]
    [HttpDelete("admin/users/{id:long}")]
    public async Task<IActionResult> DeleteUser(long id)
    {
        var result = await _userService.DeleteUser(id);

        return result switch
        {
            DeleteUserResult.Success => Ok(new { status = "Deleted" }),
            DeleteUserResult.NotFound => NotFound(new { error = "User not found" }),
            DeleteUserResult.Error err => StatusCode(StatusCodes.Status500InternalServerError, new { error = err.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unexpected error during deletion." })
        };
    }

    [Authorize(Policy = "CanReadUsers")]
    [HttpGet("admin/users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _userService.GetAllUsers();

        return result switch
        {
            GetUserListResult.Success success => Ok(success.Users.Select(
                user => new AdminUserProfileDto
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
                }
            )),
            GetUserListResult.NotFound => NotFound(new { error = "No users found." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unexpected error retrieving users." })
        };
    }

    [Authorize]
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        return NoContent();
    }
}