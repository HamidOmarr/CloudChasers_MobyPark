using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.LicensePlate;
using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Services.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicensePlateController : BaseController
{
    private readonly ServiceStack _services;
    private readonly SessionService _sessionService;

    public LicensePlateController(ServiceStack services) : base(services)
    {
        _services = services;
        _sessionService = services.Sessions;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLicensePlateDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (string.IsNullOrEmpty(dto.LicensePlate))
            return BadRequest(new { error = "Required field missing" });

        // Check if license plate exists in the system
        var existingLicensePlate = await _services.LicensePlates.GetByLicensePlate(dto.LicensePlate);
        if (existingLicensePlate is null) return Conflict(new { error = "License plate already exists", data = existingLicensePlate });

        var licensePlateModel = new LicensePlateModel
        {
            LicensePlate = dto.LicensePlate,
        };

        var licensePlate = await _services.LicensePlates.CreateLicensePlate(licensePlateModel);
        // Add license plate to user's vehicles in user_plates table
        await _services.Users.AddLicensePlateToUser(user.Id, licensePlate.LicensePlate);

        return StatusCode(201, new { status = "Success", licensePlate });
    }

}