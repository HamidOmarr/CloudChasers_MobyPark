using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Requests.User;
using MobyPark.Models.DataService;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuestController : BaseController
{
    private readonly IDataAccess _dataAccess;

    public GuestController(IDataAccess dataAccess, MobyPark.Services.SessionService sessions) : base(sessions)
    {
        _dataAccess = dataAccess;
    }

    [Authorize]
    [HttpPost("convert-guest")]
    public async Task<IActionResult> ConvertGuest([FromBody] ConvertGuestRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var current = GetCurrentUser();

        if (!string.Equals(current.Role, "GUEST", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only guest users can be converted." });

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { error = "All fields are required: username, name, email, phone." });

        // Username uniqueness
        var byUsername = await _dataAccess.Users.GetByUsername(request.Username.Trim());
        if (byUsername is not null && byUsername.Id != current.Id)
            return Conflict(new { error = "Username already taken" });

        // Email uniqueness (basic normalization: trim + lowercase)
        var cleanEmail = request.Email.Trim().ToLowerInvariant();
        var byEmail = await _dataAccess.Users.GetByEmail(cleanEmail);
        if (byEmail is not null && byEmail.Id != current.Id)
            return Conflict(new { error = "Email already taken" });

        // Apply
        current.Username = request.Username.Trim();
        current.Name = request.Name.Trim();
        current.Email = cleanEmail;
        current.Phone = request.Phone.Trim();
        current.Role = "USER";

        var ok = await _dataAccess.Users.Update(current);
        if (!ok) return StatusCode(500, new { error = "Failed to update user" });

        return Ok(new { message = "Guest converted", user = new { current.Id, current.Username, current.Name, current.Email, current.Phone, current.Role } });
    }
}
