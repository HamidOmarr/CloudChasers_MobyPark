using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingLotsController : BaseController
{
    private readonly ParkingLotAccess _parkingLotAccess;
    private readonly ParkingSessionAccess _parkingSessionAccess;

    public ParkingLotsController(SessionService sessionService, ParkingLotAccess parkingLotAccess, ParkingSessionAccess parkingSessionAccess)
        : base(sessionService)
    {
        _parkingLotAccess = parkingLotAccess;
        _parkingSessionAccess = parkingSessionAccess;
    }

    // ADMIN ONLY

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ParkingLotModel lot)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        lot.CreatedAt = DateTime.UtcNow;
        await _parkingLotAccess.Create(lot);
        return StatusCode(201, new { message = "Parking lot created" });
    }

    [HttpPut("{lotId}")]
    public async Task<IActionResult> Update(int lotId, [FromBody] ParkingLotModel lot)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        var existingLot = await _parkingLotAccess.GetById(lotId);
        if (existingLot == null) return NotFound(new { error = "Parking lot not found" });

        existingLot.Name = lot.Name;
        existingLot.Location = lot.Location;
        existingLot.Tariff = lot.Tariff;
        existingLot.DayTariff = lot.DayTariff;

        await _parkingLotAccess.Update(existingLot);
        return Ok(new { message = "Parking lot modified" });
    }

    [HttpDelete("{lotId}")]
    public async Task<IActionResult> Delete(int lotId)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        var lot = await _parkingLotAccess.GetById(lotId);
        if (lot == null) return NotFound(new { error = "Parking lot not found" });

        await _parkingLotAccess.Delete(lotId);
        return Ok(new { status = "Deleted" });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        var lots = await _parkingLotAccess.GetAll();
        return Ok(lots);
    }

    [HttpDelete("{lotId}/sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(int lotId, int sessionId)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        var session = await _parkingSessionAccess.GetById(sessionId);
        if (session == null || session.ParkingLotId != lotId)
            return NotFound(new { error = "Session not found" });

        await _parkingSessionAccess.Delete(sessionId);
        return Ok(new { status = "Deleted" });
    }

    // ADMIN + USER

    [HttpGet("{lotId}")]
    public async Task<IActionResult> GetById(int lotId)
    {
        var user = GetCurrentUser();
        var lot = await _parkingLotAccess.GetById(lotId);
        if (lot == null) return NotFound(new { error = "Parking lot not found" });

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

    [HttpGet("{lotId}/sessions")]
    public async Task<IActionResult> GetSessions(int lotId)
    {
        var user = GetCurrentUser();
        var sessions = await _parkingSessionAccess.GetByParkingLotId(lotId);

        // Users can only view their own sessions
        if (user.Role != "ADMIN")
            sessions = sessions.Where(s => s.User == user.Username).ToList();

        return Ok(sessions);
    }

    [HttpGet("{lotId}/sessions/{sessionId}")]
    public async Task<IActionResult> GetSession(int lotId, int sessionId)
    {
        var user = GetCurrentUser();
        var session = await _parkingSessionAccess.GetById(sessionId);

        if (session == null || session.ParkingLotId != lotId)
            return NotFound(new { error = "Session not found" });

        if (user.Role != "ADMIN" && session.User != user.Username)
            return Forbid();

        return Ok(session);
    }
}
