using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Requests.User;
using MobyPark.Models.DataService;
using MobyPark.Models.Repositories;
using MobyPark.Models.Repositories.RepositoryStack;
using MobyPark.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuestController : BaseController
{
    private readonly UserService _users;
    private readonly UserPlateService _userPlates;
    private readonly ParkingSessionService _parkingSession;

    public GuestController(UserService users, UserPlateService userPlates, ParkingSessionService parkingSession) : base(users)
    {
        _users = users;
        _userPlates = userPlates;
        _parkingSession = parkingSession;
    }


    // TODO: Needs changed. GUESTS do not exist. Use license plate to identify guest sessions.
    [Authorize]
    [HttpPost("convert-guest")]
    public async Task<IActionResult> ConvertGuest([FromBody] ConvertGuestRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var current = await GetCurrentUserAsync();

        // Check if license plate is only associated with guest user
        var userPlates = await _userPlates.GetUserPlatesByPlate(request.LicensePlate.Trim());

        // Check if any of the user plates belong to a non-guest user and has had a session recently
        foreach (var up in userPlates)
        {
            var user = await _users.GetUserById(up.UserId);
            if (user is not null && user.Id != UserRepository.DeletedUserId)
            {
                var hasRecentSession =
                    (await _parkingSession.GetAllRecentParkingSessionsByLicensePlate(up.LicensePlateNumber, TimeSpan.FromDays(30))).Count != 0;

                if (hasRecentSession)
                    return BadRequest(new { error = "License plate is already associated with another user." });
            }

        }

    }


    // [Authorize]
    // [HttpPost("convert-guest")]
    // public async Task<IActionResult> ConvertGuest([FromBody] ConvertGuestRequest request)
    // {
    //     if (!ModelState.IsValid) return BadRequest(ModelState);
    //     var current = GetCurrentUser();
    //
    //     if (!string.Equals(current.Role, "GUEST", StringComparison.OrdinalIgnoreCase))
    //         return BadRequest(new { error = "Only guest users can be converted." });
    //
    //     if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Name) ||
    //         string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Phone))
    //         return BadRequest(new { error = "All fields are required: username, name, email, phone." });
    //
    //     // Username uniqueness
    //     var byUsername = await _users.GetUserByUsername(request.Username.Trim());
    //     if (byUsername is not null && byUsername.Id != current.Id)
    //         return Conflict(new { error = "Username already taken" });
    //
    //     // Email uniqueness (basic normalization: trim + lowercase)
    //     var cleanEmail = request.Email.Trim().ToLowerInvariant();
    //     var byEmail = await _users.GetUserByEmail(cleanEmail);
    //     if (byEmail is not null && byEmail.Id != current.Id)
    //         return Conflict(new { error = "Email already taken" });
    //
    //     // Apply
    //     current.Username = request.Username.Trim();
    //     current.Name = request.Name.Trim();
    //     current.Email = cleanEmail;
    //     current.Phone = request.Phone.Trim();
    //     current.Role = "USER";
    //
    //     var ok = await _users.UpdateUser(current);
    //     if (!ok) return StatusCode(500, new { error = "Failed to update user" });
    //
    //     return Ok(new { message = "Guest converted", user = new { current.Id, current.Username, current.Name, current.Email, current.Phone, current.Role } });
    // }
}
