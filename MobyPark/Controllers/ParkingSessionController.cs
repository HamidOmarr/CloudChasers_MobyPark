using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.Models;
using MobyPark.Services;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.ParkingSession;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingSessionController : BaseController
{
    private readonly IParkingSessionService _parkingSessions;
    private readonly IAuthorizationService _authorizationService;

    public ParkingSessionController(UserService users, IParkingSessionService parkingSessions, IAuthorizationService authorizationService) : base(users)
    {
        _parkingSessions = parkingSessions;
        _authorizationService = authorizationService;
    }

    // [HttpPost("{lotId}/sessions:start")] // start endpoint unified // Commented out as it is unclear why this was added.
    [HttpPost("{lotId}/sessions/start")]
    public async Task<IActionResult> StartSession(int lotId, [FromBody] StartParkingSessionDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        var sessionDto = new CreateParkingSessionDto
        {
            ParkingLotId = lotId,
            LicensePlate = request.LicensePlate
        };

        var result = await _parkingSessions.StartSession(
            sessionDto,
            request.CardToken,
            request.EstimatedAmount,
            user.Username,
            request.SimulateInsufficientFunds
        );

        return result switch
        {
            StartSessionResult.Success success => StatusCode(201, new
            {
                status = "Started",
                sessionId = success.Session.Id,
                licensePlate = success.Session.LicensePlateNumber,
                parkingLotId = success.Session.ParkingLotId,
                startedAt = success.Session.Started,
                paymentStatus = success.Session.PaymentStatus,
                availableSpots = success.AvailableSpots
            }),
            StartSessionResult.LotNotFound => NotFound(new { error = "Parking lot not found" }),
            StartSessionResult.LotFull => Conflict(new { error = "Parking lot is full", code = "LOT_FULL" }),
            StartSessionResult.AlreadyActive => Conflict(new { error = "An active session already exists for this license plate", code = "ACTIVE_SESSION_EXISTS" }),
            StartSessionResult.PreAuthFailed f => StatusCode(402, new { error = f.Reason, code = "PAYMENT_DECLINED" }),
            StartSessionResult.Error e => StatusCode(StatusCodes.Status500InternalServerError, new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
        };
    }

    [HttpPost("{lotId}/sessions/stop")]
    public async Task<IActionResult> StopSession(int lotId, [FromBody] StopParkingSessionDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _parkingSessions.StopSession(request);

        return result switch
        {
            StopSessionResult.Success success => Ok(new
            {
                status = "Stopped",
                sessionId = success.Session.Id,
                licensePlate = success.Session.LicensePlateNumber,
                parkingLotId = success.Session.ParkingLotId,
                startedAt = success.Session.Started,
                stoppedAt = success.Session.Stopped,
                paymentStatus = success.Session.PaymentStatus
            }),
            StopSessionResult.LotNotFound => NotFound(new { error = "Parking lot not found" }),
            StopSessionResult.LicensePlateNotFound => NotFound(new { error = "Active session for the provided license plate not found in this lot" }),
            StopSessionResult.AlreadyStopped => BadRequest(new { error = "The parking session has already been stopped" }),
            StopSessionResult.PaymentFailed f => StatusCode(402, new { error = f.Reason, code = "PAYMENT_FAILED" }),
            StopSessionResult.ValidationFailed v => BadRequest(new { error = v.Reason }),
            StopSessionResult.Error e => StatusCode(StatusCodes.Status500InternalServerError, new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
        };
    }

    [Authorize(Policy = "CanManageParkingSessions")]
    [HttpDelete("{lotId}/sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(int lotId, int sessionId)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var getResult = await _parkingSessions.GetParkingSessionById(sessionId);
        if (getResult is not GetSessionResult.Success success || success.Session.ParkingLotId != lotId)
            return NotFound(new { error = "Session not found in this lot" });

        var deleteResult = await _parkingSessions.DeleteParkingSession(sessionId);

        return deleteResult switch
        {
            DeleteSessionResult.Success => Ok(new { status = "Deleted" }),
            DeleteSessionResult.NotFound => NotFound(new { error = "Session not found" }),
            DeleteSessionResult.Error e => StatusCode(StatusCodes.Status500InternalServerError, new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown delete error occurred." })
        };
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
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred." })
        };
    }
}
