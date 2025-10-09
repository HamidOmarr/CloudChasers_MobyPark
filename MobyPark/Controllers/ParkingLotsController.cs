using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingLotsController : BaseController
{
    private readonly ServiceStack _services;

    public ParkingLotsController(ServiceStack services) : base(services.Sessions)
    {
        _services = services;
    }

    // ADMIN ONLY

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ParkingLotModel lot)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        lot.CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
        await _services.ParkingLots.CreateParkingLot(lot);
        return StatusCode(201, new { message = "Parking lot created" });
    }

    [HttpPut("{lotId}")]
    public async Task<IActionResult> Update(int lotId, [FromBody] ParkingLotModel lot)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        var existingLot = await _services.ParkingLots.GetParkingLotById(lotId);
        if (existingLot is null) return NotFound(new { error = "Parking lot not found" });

        var newLot = new ParkingLotModel
        {
            Id = lotId,
            Name = lot.Name ?? existingLot.Name,
            Location = lot.Location ?? existingLot.Location,
            Address = lot.Address ?? existingLot.Address,
            Capacity = lot.Capacity != 0 ? lot.Capacity : existingLot.Capacity,
            Reserved = existingLot.Reserved,
            Tariff = lot.Tariff != 0 ? lot.Tariff : existingLot.Tariff,
            DayTariff = lot.DayTariff ?? existingLot.DayTariff,
            CreatedAt = existingLot.CreatedAt,
        };

        await _services.ParkingLots.UpdateParkingLot(newLot);

        return Ok(new { message = "Parking lot modified" });
    }

    [HttpDelete("{lotId}")]
    public async Task<IActionResult> Delete(int lotId)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        var lot = await _services.ParkingLots.GetParkingLotById(lotId);
        if (lot is null) return NotFound(new { error = "Parking lot not found" });

        await _services.ParkingLots.DeleteParkingLot(lotId);
        return Ok(new { status = "Deleted" });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        var lots = await _services.ParkingLots.GetAllParkingLots();
        return Ok(lots);
    }

    // ADMIN + USER

    [HttpGet("{lotId}")]
    public async Task<IActionResult> GetById(int lotId)
    {
        var user = GetCurrentUser();
        var lot = await _services.ParkingLots.GetParkingLotById(lotId);
        if (lot is null) return NotFound(new { error = "Parking lot not found" });

        // Admins get all data, users get filtered data
        if (user.Role == "ADMIN") return Ok(lot);

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
