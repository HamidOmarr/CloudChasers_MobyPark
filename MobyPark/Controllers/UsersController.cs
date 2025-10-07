using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Requests;
using MobyPark.Services;
using MobyPark.Services.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : BaseController
{
    private readonly UserService _userService;
    private readonly ServiceStack _services;

    public UsersController(ServiceStack services) : base(services.Sessions)
    {
        _userService = services.Users;
        _services = services;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        var user = await _userService.CreateUserAsync(
            req.Username, req.Password, req.Name, req.Email, req.Phone, req.Birthday);
        var token = SessionService.CreateSession(user);
        return Ok(new AuthResponse { UserId = user.Id, Username = user.Username, Email = user.Email, Token = token });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody]LoginRequest req)
    {
        try
        {
            var response = await _services.Users.LoginAsync(req.Identifier, req.Password);

            return Ok(response);
        }
        catch (InvalidOperationException)
        {
            return Unauthorized(new { error = "Invalid credentials" });
        }
    }

    /*[HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] LoginRequest request)
    {
        var user = GetCurrentUser();

        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = _services.Users.HashPassword(request.Password);

        if (!string.IsNullOrWhiteSpace(request.Username))
            user.Username = request.Username;

        await _services.Users.UpdateUser(user);
        return Ok(new { message = "User updated successfully" });
    }*/

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
