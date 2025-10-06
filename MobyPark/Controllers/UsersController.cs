using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Requests;
using MobyPark.Services.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly ServiceStack _services;

    public UsersController(ServiceStack services) : base(services.Sessions)
    {
        _services = services;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Phone) || request.BirthYear < 1900 || request.BirthYear > DateTime.Now.Year)
            return BadRequest(new { error = "Missing required fields" });

        UserModel? existing = await _services.Users.GetUserByUsername(request.Username);

        if (existing is not null)
            return Conflict(new { error = "Username already taken" });

        UserModel newUser = new UserModel
        {
            Username = request.Username,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            BirthYear = request.BirthYear,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };

        await _services.Users.CreateUser(newUser);
        return StatusCode(201, new { message = "User created" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { error = "Missing credentials" });

        UserModel? user = await _services.Users.GetUserByUsername(request.Username);
        if (user is null || !_services.Users.VerifyPassword(request.Password, user.Password))
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
            user.Password = _services.Users.HashPassword(request.Password);

        if (!string.IsNullOrWhiteSpace(request.Username))
            user.Username = request.Username;

        await _services.Users.UpdateUser(user);
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
