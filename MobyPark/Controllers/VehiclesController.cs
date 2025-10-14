using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Requests;
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

        var vehicleModel = new VehicleModel
        {
            UserId = user.Id,
            LicensePlate = request.LicensePlate,
            Make = request.Make,
            Model = request.Model,
            Color = request.Color,
            Year = request.Year,
            CreatedAt = DateTime.UtcNow
        };

        var vehicle = await _services.Vehicles.CreateVehicle(vehicleModel);

        return StatusCode(201, new { status = "Success", vehicle });
    }

    [HttpPost("{licensePlate}/entry")]
    public async Task<IActionResult> GetVehicleEntry(string licensePlate, [FromBody] VehicleEntryRequest request)
    {
        var user = GetCurrentUser();
        var vehicle = await _services.Vehicles.GetVehicleByUserIdAndLicense(user.Id, licensePlate);

        if ((UserRole)user.RoleId <= UserRole.Employee)
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

        if ((UserRole)user.RoleId <= UserRole.Employee)
            vehicle = await _services.Vehicles.GetVehicleByLicensePlate(request.LicensePlate);

        if (vehicle is null)
            return NotFound(new { error = "Vehicle not found" });

        if (!string.IsNullOrEmpty(request.Make))
            vehicle.Make = request.Make;
        if (!string.IsNullOrEmpty(request.Model))
            vehicle.Model = request.Model;
        if (!string.IsNullOrEmpty(request.Color))
            vehicle.Color = request.Color;
        if (request.Year > 0)
            vehicle.Year = request.Year;

        var updatedVehicle = await _services.Vehicles.UpdateVehicle(vehicle);

        return Ok(new { status = "Success", updatedVehicle });
    }

    [HttpDelete("{licensePlate}")]
    public async Task<IActionResult> DeleteVehicle(string licensePlate)
    {
        var user = GetCurrentUser();
        var vehicle = await _services.Vehicles.GetVehicleByUserIdAndLicense(user.Id, licensePlate);

        if ((UserRole)user.RoleId <= UserRole.Employee)
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

        if ((UserRole)user.RoleId <= UserRole.Employee)
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
            if ((UserRole)user.RoleId == UserRole.Employee)
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
