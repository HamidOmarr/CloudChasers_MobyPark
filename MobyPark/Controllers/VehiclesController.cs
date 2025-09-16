using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.Requests;
using MobyPark.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : BaseController
{
    private readonly VehicleAccess _vehicleAccess;
    private readonly UserAccess _userAccess;

    public VehiclesController(SessionService sessionService, VehicleAccess vehicleAccess, UserAccess userAccess)
        : base(sessionService)
    {
        _vehicleAccess = vehicleAccess;
        _userAccess = userAccess;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleRequest request)
    {
        var user = GetCurrentUser();

        if (string.IsNullOrEmpty(request.LicensePlate))
            return BadRequest(new { error = "Required field missing" });

        var existingVehicle = await _vehicleAccess.GetByUserAndLicense(user.Id, request.LicensePlate);
        if (existingVehicle != null)
            return Conflict(new { error = "Vehicle already exists", data = existingVehicle });

        var vehicle = new VehicleModel
        {
            UserId = user.Id,
            LicensePlate = request.LicensePlate,
            CreatedAt = DateTime.UtcNow
        };

        await _vehicleAccess.Create(vehicle);
        return StatusCode(201, new { status = "Success", vehicle });
    }

    [HttpPost("{licensePlate}/entry")]
    public async Task<IActionResult> GetVehicleEntry(string licensePlate, [FromBody] VehicleEntryRequest request)
    {
        var user = GetCurrentUser();
        var vehicle = await _vehicleAccess.GetByUserAndLicense(user.Id, licensePlate);

        if (user.Role == "ADMIN")
            vehicle = await _vehicleAccess.GetByLicensePlate(licensePlate);

        if (vehicle == null)
            return NotFound(new { error = "Vehicle does not exist", data = licensePlate });

        return Ok(new { status = "Accepted", vehicle });
    }

    [HttpPut("{licensePlate}")]
    public async Task<IActionResult> UpdateVehicle(string licensePlate, [FromBody] VehicleModel request)
    {
        var user = GetCurrentUser();
        var vehicle = await _vehicleAccess.GetByUserAndLicense(user.Id, licensePlate);

        if (user.Role == "ADMIN")
            vehicle = await _vehicleAccess.GetByLicensePlate(licensePlate);

        if (vehicle is null)
            return NotFound(new { error = "Vehicle not found" });

        if (string.IsNullOrWhiteSpace(request.Id.ToString()))
            return BadRequest(new { error = "Vehicle name is required" });

        vehicle.Id = request.Id;

        await _vehicleAccess.Update(vehicle);
        return Ok(new { status = "Success", vehicle });
    }

    [HttpDelete("{licensePlate}")]
    public async Task<IActionResult> DeleteVehicle(string licensePlate)
    {
        var user = GetCurrentUser();
        var vehicle = await _vehicleAccess.GetByUserAndLicense(user.Id, licensePlate);

        if (user.Role == "ADMIN")
            vehicle = await _vehicleAccess.GetByLicensePlate(licensePlate);

        if (vehicle == null)
            return NotFound(new { error = "Vehicle not found" });

        await _vehicleAccess.Delete(vehicle.Id);
        return Ok(new { status = "Deleted" });
    }

    [HttpGet("{licensePlate}")]
    public async Task<IActionResult> GetVehicle(string licensePlate)
    {
        var user = GetCurrentUser();
        var vehicle = await _vehicleAccess.GetByUserAndLicense(user.Id, licensePlate);

        if (user.Role == "ADMIN")
            vehicle = await _vehicleAccess.GetByLicensePlate(licensePlate);

        if (vehicle == null)
            return NotFound(new { error = "Vehicle not found" });

        return Ok(vehicle);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserVehicles([FromQuery] string? username = null)
    {
        var user = GetCurrentUser();
        int targetUserId;

        if (!string.IsNullOrEmpty(username))
        {
            if (user.Role != "ADMIN")
                return Forbid();

            var targetUser = await _userAccess.GetByUsername(username);
            if (targetUser == null)
                return NotFound(new { error = "User not found" });

            targetUserId = targetUser.Id;
        }
        else
            targetUserId = user.Id;

        var vehicles = await _vehicleAccess.GetByUserId(targetUserId);
        return Ok(vehicles);
    }
}
