using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.DTOs.Reservation.Request;
using MobyPark.DTOs.Reservation.Response;
using MobyPark.DTOs.Shared;
using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Reservation;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.Controllers;
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReservationsController : BaseController
{
    private readonly IReservationService _reservations;
    private readonly IAuthorizationService _authorization;

    public ReservationsController(
        IUserService users,
        IReservationService reservations,
        IAuthorizationService authorizationService)
        : base(users)
    {
        _reservations = reservations;
        _authorization = authorizationService;
    }

    [Authorize]
    [HttpPost]
    [SwaggerOperation(Summary = "Creates a new parking reservation.")]
    [SwaggerResponse(201, "Reservation created", typeof(ReservationResponseDto))]
    [SwaggerResponse(400, "Invalid input or time window")]
    [SwaggerResponse(403, "Not authorized to reserve for this user")]
    [SwaggerResponse(404, "Parking lot, license plate, or user not found")]
    [SwaggerResponse(409, "Parking lot is full or reservation overlap exists")]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto request)
    {
        if (request.StartDate == default || request.EndDate == default || request.StartDate >= request.EndDate)
            return BadRequest(new ErrorResponseDto { Error = "Valid StartDate and EndDate are required, and StartDate must be before EndDate." });

        var user = await GetCurrentUserAsync();
        bool isAdminRequest = false;

        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            var authorizationResult = await _authorization.AuthorizeAsync(User, "CanManageReservations");
            if (!authorizationResult.Succeeded)
                return Forbid();
            isAdminRequest = true;
        }

        var result = await _reservations.CreateReservation(request, user.Id, isAdminRequest);

        return result switch
        {
            CreateReservationResult.Success s => CreatedAtAction(nameof(GetReservation),
                new StatusResponseDto { Status = s.Reservation.Id.ToString() }, s.Reservation),
            CreateReservationResult.LotNotFound => NotFound(new ErrorResponseDto { Error = "Parking lot not found." }),
            CreateReservationResult.PlateNotFound => NotFound(new ErrorResponseDto { Error = "License plate not found." }),
            CreateReservationResult.UserNotFound notFound => NotFound(new ErrorResponseDto { Error = $"User '{notFound.Username}' not found." }),
            CreateReservationResult.PlateNotOwned notOwned => Unauthorized(new ErrorResponseDto { Error = notOwned.Message }),
            CreateReservationResult.LotFull => Conflict(new ErrorResponseDto { Error = "Parking lot is full for the selected window", Data = "LOT_FULL" }),
            CreateReservationResult.Forbidden => Forbid(),
            CreateReservationResult.InvalidInput i => BadRequest(new ErrorResponseDto { Error = i.Message }),
            CreateReservationResult.AlreadyExists a => Conflict(new ErrorResponseDto { Error = a.Message }),
            CreateReservationResult.Error e => StatusCode(500, new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [Authorize]
    [HttpPut("{reservationId:long}")]
    [SwaggerOperation(Summary = "Updates an existing reservation.")]
    [SwaggerResponse(200, "Update successful", typeof(ReservationModel))]
    [SwaggerResponse(404, "Reservation not found")]
    public async Task<IActionResult> UpdateReservation(long reservationId, [FromBody] UpdateReservationDto dto)
    {
        var user = await GetCurrentUserAsync();

        if (dto.Status.HasValue)
        {
            var authResult = await _authorization.AuthorizeAsync(User, "CanManageReservations");
            if (!authResult.Succeeded)
            {
                var getResult = await _reservations.GetReservationById(reservationId, user.Id);
                if (getResult is GetReservationResult.Success s && s.Reservation.Status != dto.Status.Value)
                    return Forbid();
            }
        }

        var updateResult = await _reservations.UpdateReservation(reservationId, user.Id, dto);

        return updateResult switch
        {
            UpdateReservationResult.Success s => Ok(s.Reservation),
            UpdateReservationResult.NoChangesMade => Ok(new StatusResponseDto { Status = "No changes made to the reservation." }),
            UpdateReservationResult.NotFound => NotFound(new ErrorResponseDto { Error = "Reservation not found or access denied." }),
            UpdateReservationResult.Error e => StatusCode(500, new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred during update." })
        };
    }

    [Authorize]
    [HttpDelete("{reservationId:long}")]
    [SwaggerOperation(Summary = "Deletes (cancels) a reservation.")]
    [SwaggerResponse(200, "Deleted successfully")]
    [SwaggerResponse(404, "Reservation not found")]
    public async Task<IActionResult> DeleteReservation(long reservationId)
    {
        var user = await GetCurrentUserAsync();
        var result = await _reservations.DeleteReservation(reservationId, user.Id);

        return result switch
        {
            DeleteReservationResult.Success => Ok(new StatusResponseDto { Status = "Deleted" }),
            DeleteReservationResult.NotFound => NotFound(new ErrorResponseDto { Error = "Reservation not found or access denied." }),
            DeleteReservationResult.Forbidden => Forbid(),
            DeleteReservationResult.Error e => BadRequest(new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred during deletion." })
        };
    }

    [Authorize]
    [HttpGet("{reservationId:long}")]
    [SwaggerOperation(Summary = "Retrieves a specific reservation.")]
    [SwaggerResponse(200, "Reservation found", typeof(ReservationModel))]
    [SwaggerResponse(404, "Reservation not found")]
    public async Task<IActionResult> GetReservation(long reservationId)
    {
        var user = await GetCurrentUserAsync();
        var result = await _reservations.GetReservationById(reservationId, user.Id);

        return result switch
        {
            GetReservationResult.Success success => Ok(success.Reservation),
            GetReservationResult.NotFound => NotFound(new ErrorResponseDto { Error = "Reservation not found or access denied." }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [Authorize(Policy = "CanManageReservations")]
    [HttpGet]
    [SwaggerOperation(Summary = "Retrieves all reservations (Admin only).")]
    [SwaggerResponse(200, "List retrieved", typeof(List<ReservationModel>))]
    public async Task<IActionResult> GetAllReservations()
    {
        var result = await _reservations.GetAllReservations();
        return result switch
        {
            GetReservationListResult.Success s => Ok(s.Reservations),
            GetReservationListResult.NotFound => Ok(new List<ReservationModel>()),
            _ => StatusCode(500, new ErrorResponseDto { Error = "An unknown error occurred." })
        };
    }

    [Authorize]
    [HttpPost("estimate")]
    [SwaggerOperation(Summary = "Estimates the cost of a reservation without creating it.")]
    [SwaggerResponse(200, "Estimate calculated", typeof(ReservationCostEstimateResponseDto))]
    [SwaggerResponse(400, "Invalid time window or lot closed")]
    [SwaggerResponse(404, "Parking lot not found")]
    public async Task<IActionResult> Estimate([FromBody] ReservationCostEstimateRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();
        var result = await _reservations.GetReservationCostEstimate(dto, user.Id);

        return result switch
        {
            GetReservationCostEstimateResult.Success s => Ok(new ReservationCostEstimateResponseDto { EstimatedCost = s.EstimatedCost }),
            GetReservationCostEstimateResult.LotNotFound => NotFound(new ErrorResponseDto { Error = "Parking lot not found" }),
            GetReservationCostEstimateResult.InvalidTimeWindow w => BadRequest(new ErrorResponseDto { Error = w.Reason }),
            GetReservationCostEstimateResult.LotClosed => BadRequest(new ErrorResponseDto { Error = "Parking lot is closed for the selected time window" }),
            GetReservationCostEstimateResult.Error e => StatusCode(500, new ErrorResponseDto { Error = e.Message }),
            _ => StatusCode(500, new ErrorResponseDto { Error = "Unknown estimate error." })
        };
    }
}