using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.Requests;
using MobyPark.Services;
using MobyPark.Services.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : BaseController
{
    private readonly ServiceStack _services;

    public VehiclesController(ServiceStack services) : base(services.Sessions)
    {
        _services = services;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleRequest request)
    {
        var user = GetCurrentUser();

        if (string.IsNullOrEmpty(request.LicensePlate))
            return BadRequest(new { error = "Required field missing" });

        var existingVehicle = await _services.Vehicles.GetVehicleByUserIdAndLicense(user.Id, request.LicensePlate);
        if (existingVehicle is not null) return Conflict(new { error = "Vehicle already exists", data = existingVehicle });


        var vehicle = await _services.Vehicles.CreateVehicle(user.Id, request.LicensePlate, request.Make, request.Model,
            request.Color, request.Year);

        return StatusCode(201, new { status = "Success", vehicle });
    }

    [HttpPost("{licensePlate}/entry")]
    public async Task<IActionResult> GetVehicleEntry(string licensePlate, [FromBody] VehicleEntryRequest request)
    {
        var user = GetCurrentUser();
        var vehicle = await _services.Vehicles.GetVehicleByUserIdAndLicense(user.Id, licensePlate);

        if (user.Role == "ADMIN")
            vehicle = await _services.Vehicles.GetVehicleByLicensePlate(licensePlate);

        if (vehicle is null)
            return NotFound(new { error = "Vehicle does not exist", data = licensePlate });

        //TODO: Check the parking lot in request for open spaces

        return Ok(new { status = "Accepted", vehicle });
    }

    [HttpPut("{licensePlate}")]
    public async Task<IActionResult> UpdateVehicle([FromBody] VehicleUpdateRequest request)
    {
        var user = GetCurrentUser();
        var vehicle = await _services.Vehicles.GetVehicleByUserIdAndLicense(user.Id, request.LicensePlate);

        if (user.Role == "ADMIN")
            vehicle = await _services.Vehicles.GetVehicleByLicensePlate(request.LicensePlate);

        if (vehicle is null)
            return NotFound(new { error = "Vehicle not found" });

        var updatedVehicle = await _services.Vehicles.UpdateVehicle(user.Id, request.LicensePlate, request.Make,
            request.Model, request.Color, request.Year);

        return Ok(new { status = "Success", updatedVehicle });
    }

    [HttpDelete("{licensePlate}")]
    public async Task<IActionResult> DeleteVehicle(string licensePlate)
    {
        var user = GetCurrentUser();
        var vehicle = await _services.Vehicles.GetVehicleByUserIdAndLicense(user.Id, licensePlate);

        if (user.Role == "ADMIN")
            vehicle = await _services.Vehicles.GetVehicleByLicensePlate(licensePlate);

        if (vehicle == null)
            return NotFound(new { error = "Vehicle not found" });

        await _services.Vehicles.DeleteVehicle(vehicle.Id);

        return Ok(new { status = "Deleted" });
    }

    [HttpGet("{licensePlate}")]
    public async Task<IActionResult> GetVehicle(string licensePlate)
    {
        var user = GetCurrentUser();
        var vehicle = await _services.Vehicles.GetVehicleByUserIdAndLicense(user.Id, licensePlate);

        if (user.Role == "ADMIN")
            vehicle = await _services.Vehicles.GetVehicleByLicensePlate(licensePlate);

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

            var targetUser = await _services.Users.GetUserByUsername(username);
            if (targetUser == null)
                return NotFound(new { error = "User not found" });

            targetUserId = targetUser.Id;
        }
        else
            targetUserId = user.Id;

        var vehicles = await _services.Vehicles.GetVehicleByUserId(targetUserId);
        return Ok(vehicles);
    }
}
