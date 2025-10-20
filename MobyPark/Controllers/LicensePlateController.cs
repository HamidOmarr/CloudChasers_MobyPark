using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.LicensePlate;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicensePlateController : BaseController
{
    private readonly LicensePlateService _licensePlates;
    private readonly UserPlateService _userPlates;

    public LicensePlateController(UserService users, LicensePlateService licensePlates, UserPlateService userPlates) : base(users)
    {
        _licensePlates = licensePlates;
        _userPlates = userPlates;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLicensePlateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();
        if (string.IsNullOrEmpty(dto.LicensePlate))
            return BadRequest(new { error = "Required field missing" });

        var existingLicensePlate = await _licensePlates.GetByLicensePlate(dto.LicensePlate);
        if (existingLicensePlate is null) return Conflict(new { error = "License plate already exists", data = existingLicensePlate });

        var licensePlateModel = new LicensePlateModel { LicensePlateNumber = dto.LicensePlate };

        var licensePlate = await _licensePlates.CreateLicensePlate(licensePlateModel);
        await _userPlates.AddLicensePlateToUser(user.Id, licensePlate.LicensePlateNumber);

        return StatusCode(201, new { status = "Success", licensePlate });
    }

}