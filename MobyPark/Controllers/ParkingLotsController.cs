using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingLotsController : BaseController
{
    private readonly ParkingLotService _parkingLots;
    private readonly IAuthorizationService _authorizationService;

    public ParkingLotsController(UserService users, ParkingLotService parkingLots, IAuthorizationService authorizationService) : base(users)
    {
        _parkingLots = parkingLots;
        _authorizationService = authorizationService;
    }

    [Authorize(Policy = "CanManageParkingLots")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ParkingLotModel lot)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        lot.CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
        await _parkingLots.CreateParkingLot(lot);
        return StatusCode(201, new { message = "Parking lot created" });
    }

    [Authorize(Policy = "CanManageParkingLots")]
    [HttpPut("{lotId}")]
    public async Task<IActionResult> Update(int lotId, [FromBody] ParkingLotModel lot)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingLot = await _parkingLots.GetParkingLotById(lotId);
        if (existingLot is null) return NotFound(new { error = "Parking lot not found" });

        var newLot = new ParkingLotModel
        {
            Id = lotId,
            Name = lot.Name,
            Location = lot.Location,
            Address = lot.Address,
            Capacity = lot.Capacity != 0 ? lot.Capacity : existingLot.Capacity,
            Reserved = existingLot.Reserved,
            Tariff = lot.Tariff != 0 ? lot.Tariff : existingLot.Tariff,
            DayTariff = lot.DayTariff ?? existingLot.DayTariff,
            CreatedAt = existingLot.CreatedAt,
        };

        await _parkingLots.UpdateParkingLot(newLot);
        return Ok(new { message = "Parking lot modified" });
    }

    [Authorize(Policy = "CanManageParkingLots")]
    [HttpDelete("{lotId}")]
    public async Task<IActionResult> Delete(int lotId)
    {
        var lot = await _parkingLots.GetParkingLotById(lotId);
        if (lot is null) return NotFound(new { error = "Parking lot not found" });

        await _parkingLots.DeleteParkingLot(lotId);
        return Ok(new { status = "Deleted" });
    }

    [Authorize(Policy = "CanManageParkingLots")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var lots = await _parkingLots.GetAllParkingLots();
        return Ok(lots);
    }

    // ADMIN + USER
    [Authorize(Policy = "CanReadParkingLots")]
    [HttpGet("{lotId}")]
    public async Task<IActionResult> GetById(int lotId)
    {
        var lot = await _parkingLots.GetParkingLotById(lotId);
        if (lot is null) return NotFound(new { error = "Parking lot not found" });
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, "CanManageParkingLots");

        // Admins get all data, users get filtered data
        if (authorizationResult.Succeeded)
            return Ok(lot);

        int spotsAvailable = lot.Capacity - lot.Reserved;
        return Ok(new
        {
            lot.Name,
            lot.Location,
            lot.Address,
            lot.Tariff,
            lot.DayTariff,
            spotsAvailable
        });
    }
}
