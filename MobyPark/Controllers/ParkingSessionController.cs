using Microsoft.AspNetCore.Mvc;
using MobyPark.Services.Services;

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
