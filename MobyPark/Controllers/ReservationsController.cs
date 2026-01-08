using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.DTOs.Reservation.Request;
using MobyPark.Models;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results.Reservation;
using MobyPark.Services;

namespace MobyPark.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ReservationsController : BaseController
{
    private readonly IReservationService _reservations;
    private readonly IAuthorizationService _authorization;

    public ReservationsController(IUserService users, IReservationService reservations, IAuthorizationService authorizationService) : base(users)
    {
        _reservations = reservations;
        _authorization = authorizationService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (request.StartDate == default || request.EndDate == default || request.StartDate >= request.EndDate)
            return BadRequest(new { error = "Valid StartDate and EndDate are required, and StartDate must be before EndDate." });

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
                                                                new { reservationId = s.Reservation.Id },
                                                                s.Reservation),
            CreateReservationResult.LotNotFound => NotFound(new { error = "Parking lot not found." }),
            CreateReservationResult.PlateNotFound => NotFound(new { error = "License plate not found." }),
            CreateReservationResult.UserNotFound notFound => NotFound(new { error = $"User '{notFound.Username}' not found." }),
            CreateReservationResult.PlateNotOwned notOwned => Unauthorized(new { error = notOwned.Message }),
            CreateReservationResult.LotFull => Conflict(new { error = "Parking lot is full for the selected window", code = "LOT_FULL" }),
            CreateReservationResult.Forbidden => Forbid(),
            CreateReservationResult.InvalidInput i => BadRequest(new { error = i.Message }),
            CreateReservationResult.AlreadyExists a => Conflict(new { error = a.Message }),
            CreateReservationResult.Error e => StatusCode(StatusCodes.Status500InternalServerError, new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
        };
    }

    [Authorize]
    [HttpPut("{reservationId}")]
    public async Task<IActionResult> UpdateReservation(long reservationId, [FromBody] UpdateReservationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

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
            UpdateReservationResult.NoChangesMade => Ok(new { status = "No changes made to the reservation." }),
            UpdateReservationResult.NotFound => NotFound(new { error = "Reservation not found or access denied." }),
            UpdateReservationResult.Error e => BadRequest(new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred during update." })
        };
    }

    [Authorize]
    [HttpDelete("{reservationId}")]
    public async Task<IActionResult> DeleteReservation(long reservationId)
    {
        var user = await GetCurrentUserAsync();
        var result = await _reservations.DeleteReservation(reservationId, user.Id);

        return result switch
        {
            DeleteReservationResult.Success => Ok(new { status = "Deleted" }),
            DeleteReservationResult.NotFound => NotFound(new { error = "Reservation not found or access denied." }),
            DeleteReservationResult.Forbidden => Forbid(),
            DeleteReservationResult.Error e => BadRequest(new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred during deletion." })
        };
    }

    [Authorize]
    [HttpGet("{reservationId}")]
    public async Task<IActionResult> GetReservation(long reservationId)
    {
        var user = await GetCurrentUserAsync();

        var result = await _reservations.GetReservationById(reservationId, user.Id);

        return result switch
        {
            GetReservationResult.Success success => Ok(success.Reservation),
            GetReservationResult.NotFound => NotFound(new { error = "Reservation not found or access denied." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
        };
    }

    [Authorize(Policy = "CanManageReservations")]
    [HttpGet]
    public async Task<IActionResult> GetAllReservations()
    {
         var result = await _reservations.GetAllReservations();
         return result switch {
            GetReservationListResult.Success s => Ok(s.Reservations),
            GetReservationListResult.NotFound => Ok(new List<ReservationModel>()),
             _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unknown error occurred." })
         };
    }

    [Authorize]
    [HttpPost("estimate")]
    public async Task<IActionResult> Estimate([FromBody] ReservationCostEstimateRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();
        var result = await _reservations.GetReservationCostEstimate(dto, user.Id);

        return result switch
        {
            GetReservationCostEstimateResult.Success s => Ok(new { estimatedCost = s.EstimatedCost }),
            GetReservationCostEstimateResult.LotNotFound => NotFound(new { error = "Parking lot not found" }),
            GetReservationCostEstimateResult.InvalidTimeWindow w => BadRequest(new { error = w.Reason }),
            GetReservationCostEstimateResult.LotClosed => BadRequest(new { error = "Parking lot is closed for the selected time window" }),
            GetReservationCostEstimateResult.Error e => StatusCode(StatusCodes.Status500InternalServerError, new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "Unknown estimate error." })
        };
    }
}
