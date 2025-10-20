using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs;
using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.Services;
using MobyPark.Services.Exceptions;
using MobyPark.Services.Results.Session;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingSessionController : BaseController
{
    private readonly ParkingSessionService _parkingSessions;
    private readonly ParkingLotService _parkingLots;

    private readonly IAuthorizationService _authorizationService;

    public ParkingSessionController(UserService users, ParkingSessionService parkingSessions, ParkingLotService lots, IAuthorizationService authorizationService) : base(users)
    {
        _parkingSessions = parkingSessions;
        _parkingLots = lots;
        _authorizationService = authorizationService;
    }

    // [HttpPost("{lotId}/sessions:start")] // start endpoint unified // Commented out as it is unclear why this was added.
    [HttpPost("{lotId}/sessions/start")]
    public async Task<IActionResult> StartSession(int lotId, [FromBody] StartParkingSessionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var sessionDto = new ParkingSessionCreateDto
            {
                ParkingLotId = lotId,
                LicensePlate = request.LicensePlate
            };

            var session = await _parkingSessions.StartSession(
                sessionDto,
                request.CardToken,
                request.EstimatedAmount,
                GetCurrentUserAsync().Result.Username,
                request.SimulateInsufficientFunds
            );

            var lot = await _parkingLots.GetParkingLotById(lotId);
            int? available = lot?.Capacity != null ? lot.Capacity - lot.Reserved : null;

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

    [Authorize(Policy = "CanManageParkingSessions")]
    [HttpDelete("{lotId}/sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(int lotId, int sessionId)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var session = await _parkingSessions.GetParkingSessionById(sessionId);
        if (session.ParkingLotId != lotId)
            return NotFound(new { error = "Session not found" });

        await _parkingSessions.DeleteParkingSession(sessionId);
        return Ok(new { status = "Deleted" });
    }

    [Authorize]
    [HttpGet("{lotId}/sessions")]
    public async Task<IActionResult> GetSessions(int lotId)
    {
        var user = await GetCurrentUserAsync();

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, "CanManageParkingSessions");
        bool canManageSessions = authorizationResult.Succeeded;
        var sessions = await _parkingSessions.GetAuthorizedSessionsAsync(user.Id, lotId, canManageSessions);

        return Ok(sessions);
    }

    [Authorize]
    [HttpGet("{lotId}/sessions/{sessionId}")]
    public async Task<IActionResult> GetSession(int lotId, int sessionId)
    {
        var user = await GetCurrentUserAsync();

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, "CanManageParkingSessions");
        bool canManageSessions = authorizationResult.Succeeded;

        var result = await _parkingSessions.GetAuthorizedSessionAsync(
            user.Id,
            lotId,
            sessionId,
            canManageSessions
        );

        return result switch
        {
            GetSessionResult.Success(var session) => Ok(session),
            GetSessionResult.NotFound => NotFound(new { error = "Parking session not found in this lot." }),
            GetSessionResult.Forbidden => Forbid(), // Standard response for authorization failure
            _ => StatusCode(500, new { error = "An unexpected error occurred." })
        };
    }

}
