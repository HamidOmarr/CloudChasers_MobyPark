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

    [HttpPost("{lotId}/sessions:start")]
    public async Task<IActionResult> StartSession(int lotId, [FromBody] StartParkingSessionRequest request)
    {
        // Availability check + pre auth + open gate + link session
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

            return StatusCode(201, new { status = "Started", session });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Parking lot not found" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(402, new { error = ex.Message }); // 402 Payment Required placeholder
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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

    [HttpPost("{lotId}/sessions/start")]
    public async Task<IActionResult> StartSession(int lotId, [FromBody] SessionRequest data)
    {
        var user = GetCurrentUser();
        try
        {
            var newSessionId = await _services.ParkingSessions.StartParkingSession(
                lotId, data.LicensePlate, user.Username, DateTime.UtcNow);

            return Ok(new { message = $"Session started for: {data.LicensePlate}", sessionId = newSessionId });
        }
        catch (ActiveSessionAlreadyExistsException ex)
        { return BadRequest(new { error = "Cannot start a session when another session for this license plate is already started." }); }
        catch (ArgumentException ex)
        { return BadRequest(new { error = "Required field missing", field = "license plate" }); }
    }

    [HttpPost("{lotId}/sessions/stop")]
    public async Task<IActionResult> StopSession(int lotId, [FromBody] SessionRequest data)
    {
        if (string.IsNullOrWhiteSpace(data.LicensePlate))
            return BadRequest(new { error = "Required field missing", field = "licenseplate" });

        var activeSession = await _services.ParkingSessions.GetActiveSessionByLicensePlate(data.LicensePlate);

        if (activeSession == null)
            return BadRequest(new { error = "Cannot stop a session when there is no session for this licenseplate." });

        if (activeSession.ParkingLotId != lotId)
            return NotFound(new { error = "Active session not found for this parking lot and license plate." });

        await _services.ParkingSessions.StopParkingSession(activeSession.Id, DateTime.UtcNow);
        return Ok(new { message = $"Session stopped for: {data.LicensePlate}" });
    }
}
