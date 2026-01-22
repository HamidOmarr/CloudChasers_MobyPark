using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Shared;
using MobyPark.DTOs.User.Request;
using MobyPark.DTOs.User.Response;
using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Tokens;
using MobyPark.Services.Results.User;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : BaseController
{
    private readonly ITokenService _tokenService;

    public UsersController(IUserService users, ITokenService tokens) : base(users)
    {
        _tokenService = tokens;
    }

    [HttpPost("register")]
    [SwaggerOperation(Summary = "Registers a new user and returns an auth token.")]
    [SwaggerResponse(201, "User registered successfully", typeof(AuthDto))]
    [SwaggerResponse(400, "Invalid data provided")]
    [SwaggerResponse(409, "Username already taken")]
    [SwaggerResponse(500, "Server error")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _userService.CreateUserAsync(dto);

        return result switch
        {
            RegisterResult.Success success => HandleRegistrationSuccess(success.User),
            RegisterResult.UsernameTaken => Conflict(new ErrorResponseDto { Error = "Username already taken" }),
            RegisterResult.InvalidData invalid => BadRequest(new ErrorResponseDto { Error = invalid.Message }),
            RegisterResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "Unknown result" })
        };

        IActionResult HandleRegistrationSuccess(UserModel user)
        {
            var tokenResult = _tokenService.CreateToken(user);

            if (tokenResult is not CreateJwtResult.Success tokenSuccess)
                return StatusCode(500, new ErrorResponseDto { Error = "Server configuration error generating token." });

            return CreatedAtAction(
                nameof(GetUser),
                new StatusResponseDto { Message = user.Id.ToString() },
                new AuthDto
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Token = tokenSuccess.JwtToken,
                });
        }
    }

    [HttpPost("login")]
    [SwaggerOperation(Summary = "Authenticates a user and returns a JWT.")]
    [SwaggerResponse(200, "Login successful", typeof(AuthDto))]
    [SwaggerResponse(400, "Invalid input")]
    [SwaggerResponse(401, "Invalid credentials")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _userService.Login(dto);

        return result switch
        {
            LoginResult.Success s => Ok(s.Response),
            LoginResult.InvalidCredentials or LoginResult.Error =>
                Unauthorized(new ErrorResponseDto { Error = "Invalid credentials" }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "Unexpected error" })
        };
    }

    [HttpPut("profile")]
    [Authorize]
    [SwaggerOperation(Summary = "Updates the authenticated user's profile.")]
    [SwaggerResponse(200, "Profile updated", typeof(UserProfileDto))]
    [SwaggerResponse(400, "Invalid data")]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(409, "Username or Email taken")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto)
    {
        var user = await GetCurrentUserAsync();

        var result = await _userService.UpdateUserProfile(user.Id, dto);

        return result switch
        {
            UpdateUserResult.Success success => Ok(new UserProfileDto
            {
                Id = success.User.Id,
                Username = success.User.Username,
                FirstName = success.User.FirstName,
                LastName = success.User.LastName,
                Email = success.User.Email,
                Phone = success.User.Phone,
                Birthday = success.User.Birthday
            }),
            UpdateUserResult.NoChangesMade => Ok(new StatusResponseDto { Message = "No changes made to the user profile" }),
            UpdateUserResult.NotFound => NotFound(new ErrorResponseDto { Error = "User not found" }),
            UpdateUserResult.UsernameTaken => Conflict(new ErrorResponseDto { Error = "Username already taken" }),
            UpdateUserResult.EmailTaken => Conflict(new ErrorResponseDto { Error = "Email already taken" }),
            UpdateUserResult.InvalidData invalid => BadRequest(new ErrorResponseDto { Error = invalid.Message }),
            UpdateUserResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "Unexpected error" })
        };
    }

    [HttpGet("profile")]
    [Authorize]
    [SwaggerOperation(Summary = "Retrieves the authenticated user's profile.")]
    [SwaggerResponse(200, "Profile retrieved", typeof(UserProfileDto))]
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

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanReadUsers")]
    [SwaggerOperation(Summary = "Retrieves public information of a specific user.")]
    [SwaggerResponse(200, "User found", typeof(object))]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await _userService.GetUserById(id);
        if (result is not GetUserResult.Success success)
        {
            return result switch
            {
                GetUserResult.NotFound => NotFound(new ErrorResponseDto { Error = "User not found" }),
                _ => StatusCode(500, new ErrorResponseDto { Error = "Unexpected error" })
            };
        }

        var user = success.User;
        return Ok(new UserPublicProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }

    [HttpGet("admin/users/{id:int}")]
    [Authorize(Policy = "CanReadUsers")]
    [SwaggerOperation(Summary = "Retrieves full user details (Admin).")]
    [SwaggerResponse(200, "User found", typeof(AdminUserProfileDto))]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> GetUserAdmin(int id)
    {
        var result = await _userService.GetUserById(id);
        if (result is not GetUserResult.Success success)
        {
            return result switch
            {
                GetUserResult.NotFound => NotFound(new ErrorResponseDto { Error = "User not found" }),
                _ => StatusCode(500, new ErrorResponseDto { Error = "Unexpected error" })
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

    [HttpPut("admin/users/{id:long}/identity")]
    [Authorize(Policy = "CanManageUsers")]
    [SwaggerOperation(Summary = "Updates a user's identity info (Admin).")]
    [SwaggerResponse(200, "Identity updated", typeof(AdminUserProfileDto))]
    [SwaggerResponse(400, "Invalid data")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> UpdateUserIdentity(long id, [FromBody] UpdateUserIdentityDto dto)
    {
        var result = await _userService.UpdateUserIdentity(id, dto);

        return result switch
        {
            UpdateUserResult.Success success => Ok(new AdminUserProfileDto
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
            }),
            UpdateUserResult.NoChangesMade => Ok(new StatusResponseDto { Message = "No identity changes applied to the user." }),
            UpdateUserResult.NotFound => NotFound(new ErrorResponseDto { Error = "User not found" }),
            UpdateUserResult.InvalidData invalid => BadRequest(new ErrorResponseDto { Error = invalid.Message }),
            UpdateUserResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "Unexpected error while updating user identity." })
        };
    }

    [HttpPut("admin/users/{id:long}/role")]
    [Authorize(Policy = "CanManageUsers")]
    [SwaggerOperation(Summary = "Updates a user's assigned role (Admin).")]
    [SwaggerResponse(200, "Role updated", typeof(AdminUserProfileDto))]
    [SwaggerResponse(400, "Invalid role or user data")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> UpdateUserRole(long id, [FromBody] UpdateUserRoleDto dto)
    {
        var result = await _userService.UpdateUserRole(id, dto);
        return result switch
        {
            UpdateUserResult.Success success => Ok(new AdminUserProfileDto
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
            }),
            UpdateUserResult.NoChangesMade => Ok(new StatusResponseDto { Message = "User already has the specified role." }),
            UpdateUserResult.NotFound => NotFound(new ErrorResponseDto { Error = "User not found" }),
            UpdateUserResult.InvalidData invalid => BadRequest(new ErrorResponseDto { Error = invalid.Message }),
            UpdateUserResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "Unexpected error while updating user role." })
        };
    }

    [HttpDelete("admin/users/{id:long}")]
    [Authorize(Policy = "CanManageUsers")]
    [SwaggerOperation(Summary = "Deletes a user account (Admin).")]
    [SwaggerResponse(200, "User deleted")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> DeleteUser(long id)
    {
        var result = await _userService.DeleteUser(id);

        return result switch
        {
            DeleteUserResult.Success => Ok(new StatusResponseDto { Status = "Deleted" }),
            DeleteUserResult.NotFound => NotFound(new ErrorResponseDto { Error = "User not found" }),
            DeleteUserResult.Error err => StatusCode(500, new ErrorResponseDto { Error = err.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "Unexpected error during deletion." })
        };
    }

    [HttpGet("admin/users")]
    [Authorize(Policy = "CanReadUsers")]
    [SwaggerOperation(Summary = "Lists all users (Admin).")]
    [SwaggerResponse(200, "Users retrieved", typeof(List<AdminUserProfileDto>))]
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
            GetUserListResult.NotFound => NotFound(new ErrorResponseDto { Error = "No users found." }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "Unexpected error retrieving users." })
        };
    }

    [Authorize]
    [HttpGet("logout")]
    [SwaggerOperation(Summary = "Logs out the current user (client-side action mainly).")]
    [SwaggerResponse(204, "Logout successful")]
    public IActionResult Logout()
    {
        return NoContent();
    }
}