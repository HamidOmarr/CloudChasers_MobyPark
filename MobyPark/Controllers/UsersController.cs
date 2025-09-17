using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.Requests;
using MobyPark.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly UserAccess _userAccess;
    private readonly UserService _userService;

    public UsersController(SessionService sessionService, UserAccess userAccess, UserService userService)
        : base(sessionService)
    {
        _userAccess = userAccess;
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name))
            return BadRequest(new { error = "Missing required fields" });

        var existing = await _userAccess.GetByUsername(request.Username);
        if (existing != null)
            return Conflict(new { error = "Username already taken" });

        var hashedPassword = _userService.HashPassword(request.Password);

        var newUser = new UserModel
        {
            Username = request.Username,
            Password = hashedPassword,
            Name = request.Name,
            Role = "USER"
        };

        await _userAccess.Create(newUser);
        return StatusCode(201, new { message = "User created" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { error = "Missing credentials" });

        var user = await _userAccess.GetByUsername(request.Username);
        if (user == null || !_userService.VerifyPassword(request.Password, user.Password))
            return Unauthorized(new { error = "Invalid credentials" });

        var token = Guid.NewGuid().ToString();
        SessionService.AddSession(token, user);

        return Ok(new { message = "User logged in", session_token = token });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserLoginRequest request)
    {
        var user = GetCurrentUser();

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.Password = _userService.HashPassword(request.Password);

        if (!string.IsNullOrWhiteSpace(request.Username))
            user.Username = request.Username;

        await _userAccess.Update(user);
        return Ok(new { message = "User updated successfully" });
    }

    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        var user = GetCurrentUser();
        return Ok(user);
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var token))
            return BadRequest(new { error = "Invalid session token" });

        SessionService.RemoveSession(token);
        return Ok(new { message = "User logged out" });
    }
}
