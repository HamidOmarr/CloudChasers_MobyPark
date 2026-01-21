using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Cards;

using MobyPark.DTOs.ParkingSession.Request;
using MobyPark.DTOs.ParkingSession.Response;
using MobyPark.DTOs.Shared;
using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.ParkingSession;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]/{lotId:long}/sessions")]
[Produces("application/json")]
public class ParkingSessionController : BaseController
{
    private readonly IParkingSessionService _parkingSessions;
    private readonly IAuthorizationService _authorizationService;

    public ParkingSessionController(
    IUserService users,
    IParkingSessionService parkingSessions,
    IAuthorizationService authorizationService)
        : base(users)
    {
        _parkingSessions = parkingSessions;
        _authorizationService = authorizationService;
    }

    [HttpPost("start")]
    [SwaggerOperation(Summary = "Starts a new parking session.")]
    [SwaggerResponse(201, "Session started successfully", typeof(StartSessionResponseDto))]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(402, "Payment pre-authorization failed")]
    [SwaggerResponse(404, "Parking lot not found")]
    [SwaggerResponse(409, "Parking lot full or session already active")]
    public async Task<IActionResult> StartSession(long lotId, [FromBody] CreateParkingSessionDto sessionDto)
    {
        if (lotId != sessionDto.ParkingLotId)
            return BadRequest(new { error = "Parking lot ID in the URL does not match the ID in the request body." });

        var result = await _parkingSessions.StartSession(sessionDto);
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
            StartSessionResult.PaymentRequired p => StatusCode(
                StatusCodes.Status402PaymentRequired, new { error = p.Reason, code = "PAYMENT_REQUIRED" }),
            StartSessionResult.Error e => StatusCode(500, new { error = e.Message }),
            _ => StatusCode(500, new { error = "An unknown error occurred." })
        };
    }


    [HttpPost("startPaid")]
    [SwaggerOperation(Summary = "Starts a new paid parking session with card information.")]
    [SwaggerResponse(201, "Session started successfully", typeof(StartSessionResponseDto))]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(402, "Payment pre-authorization failed")]
    [SwaggerResponse(404, "Parking lot not found")]
    [SwaggerResponse(409, "Parking lot full or session already active")]
    public async Task<IActionResult> StartPaidSession(string licensePlate, long lotId, [FromBody] CreateCardInfoDto cardInfo)
    {
        var result = await _parkingSessions.StartPaidSession(licensePlate, lotId, cardInfo);
        return result switch
        {
            StartSessionResult.Success success => StatusCode(201, new StartSessionResponseDto
            {
                Status = "Started",
                SessionId = success.Session.Id,
                LicensePlate = success.Session.LicensePlateNumber,
                ParkingLotId = success.Session.ParkingLotId,
                StartedAt = success.Session.Started,
                PaymentStatus = success.Session.PaymentStatus.ToString(),
                AvailableSpots = success.AvailableSpots
            }),
            StartSessionResult.LotNotFound => NotFound(new ErrorResponseDto { Error = "Parking lot not found" }),
            StartSessionResult.LotFull => Conflict(new ErrorResponseDto { Error = "Parking lot is full", Data = "LOT_FULL" }),
            StartSessionResult.AlreadyActive => Conflict(new ErrorResponseDto { Error = "An active session already exists for this license plate", Data = "ACTIVE_SESSION_EXISTS" }),
            StartSessionResult.PreAuthFailed f => StatusCode(402, new ErrorResponseDto { Error = f.Reason, Data = "PAYMENT_DECLINED" }),
            StartSessionResult.Error e => StatusCode(500, new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [HttpPost("stop")]
    [SwaggerOperation(Summary = "Stops an active parking session and processes payment.")]
    [SwaggerResponse(200, "Session stopped and invoice generated", typeof(StopSessionResponseDto))]
    [SwaggerResponse(400, "Validation failed or already stopped")]
    [SwaggerResponse(402, "Payment failed")]
    [SwaggerResponse(404, "Session or Lot not found")]
    public async Task<IActionResult> StopSession(long sessionId, long lotId)
    {
        var result = await _parkingSessions.StopSession(sessionId);

        return result switch
        {
            StopSessionResult.Success success => Ok(new StopSessionResponseDto
            {
                Status = "Stopped",
                SessionId = success.Session.Id,
                LicensePlate = success.Session.LicensePlateNumber,
                ParkingLotId = success.Session.ParkingLotId,
                StartedAt = success.Session.Started,
                StoppedAt = success.Session.Stopped,
                PaymentStatus = success.Session.PaymentStatus.ToString(),
                Invoice = new SessionInvoiceDto
                {
                    Id = success.Invoice.Id,
                    SessionDuration = success.Invoice.SessionDuration,
                    TotalCost = success.Invoice.Cost ?? 0m,
                    CreatedAt = success.Invoice.CreatedAt,
                    Status = success.Invoice.Status.ToString(),
                    InvoiceSummary = success.Invoice.InvoiceSummary
                }
            }),
            StopSessionResult.LotNotFound => NotFound(new ErrorResponseDto { Error = "Parking lot not found" }),
            StopSessionResult.LicensePlateNotFound => NotFound(new ErrorResponseDto { Error = "Active session for the provided license plate not found in this lot" }),
            StopSessionResult.AlreadyStopped => BadRequest(new ErrorResponseDto { Error = "The parking session has already been stopped" }),
            StopSessionResult.PaymentFailed f => StatusCode(402, new ErrorResponseDto { Error = f.Reason, Data = "PAYMENT_FAILED" }),
            StopSessionResult.ValidationFailed v => BadRequest(new ErrorResponseDto { Error = v.Reason }),
            StopSessionResult.Error e => StatusCode(500, new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }



    [HttpDelete("{sessionId}")]
    [Authorize(Policy = "CanManageParkingSessions")]
    [SwaggerOperation(Summary = "Deletes a parking session record (Admin only).")]
    [SwaggerResponse(200, "Deleted successfully")]
    [SwaggerResponse(404, "Session not found")]
    public async Task<IActionResult> DeleteSession(long lotId, long sessionId)
    {
        var getResult = await _parkingSessions.GetParkingSessionById(sessionId);
        if (getResult is not GetSessionResult.Success success || success.Session.ParkingLotId != lotId)
            return NotFound(new ErrorResponseDto { Error = "Session not found in this lot" });

        var deleteResult = await _parkingSessions.DeleteParkingSession(sessionId);

        return deleteResult switch
        {
            DeleteSessionResult.Success => Ok(new StatusResponseDto { Status = "Deleted" }),
            DeleteSessionResult.NotFound => NotFound(new ErrorResponseDto { Error = "Session not found" }),
            DeleteSessionResult.Error e => StatusCode(500, new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown delete error occurred." })
        };
    }

    [HttpGet]
    [Authorize]
    [SwaggerOperation(Summary = "Retrieves sessions for a lot (Users see their own, Admins see all).")]
    [SwaggerResponse(200, "List retrieved", typeof(List<ParkingSessionModel>))]
    public async Task<IActionResult> GetSessions(long lotId)
    {
        var user = await GetCurrentUserAsync();

        var authorizationResult = await _authorizationService.AuthorizeAsync(User, "CanManageParkingSessions");
        bool canManageSessions = authorizationResult.Succeeded;

        var sessions =
            await _parkingSessions.GetAuthorizedSessionsAsync(user.Id, lotId, canManageSessions);

        return Ok(sessions);
    }

    [Authorize]
    [HttpGet("{sessionId:long}")]
    [SwaggerOperation(Summary = "Retrieves a specific parking session.")]
    [SwaggerResponse(200, "Session found", typeof(ParkingSessionModel))]
    [SwaggerResponse(403, "Not authorized to view this session")]
    [SwaggerResponse(404, "Session not found")]
    public async Task<IActionResult> GetSession(long lotId, long sessionId)
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
            GetSessionResult.NotFound => NotFound(new ErrorResponseDto { Error = "Parking session not found in this lot." }),
            GetSessionResult.Forbidden => Forbid(),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unexpected error occurred." })
        };
    }
}