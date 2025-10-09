using Microsoft.AspNetCore.Mvc;
using MobyPark.Services.Services;
using MobyPark.Models.Requests;
using MobyPark.Services;
using MobyPark.Services.Exceptions;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingSessionController : BaseController
{
    private readonly ServiceStack _services;

    public ParkingSessionController(ServiceStack services) : base(services.Sessions)
    {
        _services = services;
    }

    [HttpPost("{lotId}/sessions:start")] // start endpoint unified
    [HttpPost("{lotId}/sessions/start")] 
    public async Task<IActionResult> StartSession(int lotId, [FromBody] StartParkingSessionRequest request)
    {
        try
        {
            var session = await _services.ParkingSessions.StartSession(
                lotId,
                request.LicensePlate,
                request.CardToken,
                request.EstimatedAmount,
                GetCurrentUser()?.Username,
                request.SimulateInsufficientFunds
            );

            var lot = await _services.ParkingLots.GetParkingLotById(lotId);
            int? available = lot?.Capacity != null ? lot.Capacity - lot.Reserved : (int?)null;

            return StatusCode(201, new
            {
                status = "Started",
                sessionId = session.Id,
                licensePlate = session.LicensePlate,
                parkingLotId = session.ParkingLotId,
                startedAt = session.Started,
                paymentStatus = session.PaymentStatus,
                availableSpots = available
            });
        }
        catch (ActiveSessionAlreadyExistsException ex)
        { return Conflict(new { error = ex.Message, code = "ACTIVE_SESSION_EXISTS" }); }
        catch (KeyNotFoundException)
        { return NotFound(new { error = "Parking lot not found" }); }
        catch (UnauthorizedAccessException ex)
        { return StatusCode(402, new { error = ex.Message, code = "PAYMENT_DECLINED" }); }
        catch (InvalidOperationException ex)
        { return BadRequest(new { error = ex.Message }); }
        catch (ArgumentException ex)
        { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{lotId}/sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(int lotId, int sessionId)
    {
        var user = GetCurrentUser();
        if (user.Role != "ADMIN") return Forbid();

        var session = await _services.ParkingSessions.GetParkingSessionById(sessionId);
        if (session.ParkingLotId != lotId)
            return NotFound(new { error = "Session not found" });

        await _services.ParkingSessions.DeleteParkingSession(sessionId);
        return Ok(new { status = "Deleted" });
    }

    [HttpGet("{lotId}/sessions")]
    public async Task<IActionResult> GetSessions(int lotId)
    {
        var user = GetCurrentUser();
        var sessions = await _services.ParkingSessions.GetParkingSessionsByParkingLotId(lotId);

        // Users can only view their own sessions
        if (user.Role != "ADMIN")
            sessions = sessions.Where(session => session.User == user.Username).ToList();

        return Ok(sessions);
    }

    [HttpGet("{lotId}/sessions/{sessionId}")]
    public async Task<IActionResult> GetSession(int lotId, int sessionId)
    {
        var user = GetCurrentUser();
        var session = await _services.ParkingSessions.GetParkingSessionById(sessionId);

        if (session.ParkingLotId != lotId)
            return NotFound(new { error = "Session not found" });

        if (user.Role != "ADMIN" && session.User != user.Username)
            return Forbid();

        return Ok(session);
    }

}
