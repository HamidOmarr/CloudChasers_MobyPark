using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.Requests;
using MobyPark.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : BaseController
{
    private readonly ReservationAccess _reservationAccess;
    private readonly ParkingLotAccess _parkingLotAccess;
    private readonly VehicleAccess _vehicleAccess;
    private readonly UserAccess _userAccess;

    public ReservationsController(SessionService sessionService, ReservationAccess reservationAccess, ParkingLotAccess parkingLotAccess, VehicleAccess vehicleAccess, UserAccess userAccess)
        : base(sessionService)
    {
        _reservationAccess = reservationAccess;
        _parkingLotAccess = parkingLotAccess;
        _vehicleAccess = vehicleAccess;
        _userAccess = userAccess;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReservationRequest request)
    {
        var user = GetCurrentUser();

        if (request.StartDate == default || request.EndDate == default)
            return BadRequest(new { error = "Required fields missing" });

        var parkingLot = await _parkingLotAccess.GetById(request.ParkingLotId);
        if (parkingLot == null)
            return NotFound(new { error = "Parking lot not found" });

        var vehicle = await _vehicleAccess.GetByLicensePlate(request.LicensePlate);
        if (vehicle is null) return BadRequest(new { error = "Vehicle not found." });

        UserModel? userRequested = null;
        if (request.User is not null)
            userRequested = await _userAccess.GetByUsername(request.User);

        var reservation = new ReservationModel
        {
            ParkingLotId = request.ParkingLotId,
            VehicleId = vehicle.Id,
            StartTime = request.StartDate,
            EndTime = request.EndDate,
            UserId = user.Role == "ADMIN" && userRequested is not null ? userRequested.Id : user.Id // Ensures that if an admin makes a manual reservation, the admin user is not set as the user in reservation.
        };

        await _reservationAccess.Create(reservation);
        parkingLot.Reserved += 1;
        await _parkingLotAccess.Update(parkingLot);

        return StatusCode(201, new { status = "Success", reservation });
    }

    [HttpPut("{reservationId}")]
    public async Task<IActionResult> UpdateReservation(int reservationId, [FromBody] ReservationModel request)
    {
        var user = GetCurrentUser();
        var reservation = await _reservationAccess.GetById(reservationId);
        if (reservation == null)
            return NotFound(new { error = "Reservation not found" });

        VehicleModel? vehicle = await _vehicleAccess.GetById(request.VehicleId);
        if (vehicle is null)
            return BadRequest(new { error = "Vehicle not found" });

        if (request.StartTime == default || request.EndTime == default || request.ParkingLotId == 0)
            return BadRequest(new { error = "Required fields missing" });

        if (user.Role != "ADMIN")
            request.UserId = user.Id;

        reservation.VehicleId = request.VehicleId;
        reservation.StartTime = request.StartTime;
        reservation.EndTime = request.EndTime;
        reservation.ParkingLotId = request.ParkingLotId;
        reservation.UserId = request.UserId;

        await _reservationAccess.Update(reservation);
        return Ok(new { status = "Updated", reservation });
    }

    [HttpDelete("{reservationId}")]
    public async Task<IActionResult> DeleteReservation(int reservationId)
    {
        var user = GetCurrentUser();
        var reservation = await _reservationAccess.GetById(reservationId);
        if (reservation == null)
            return NotFound(new { error = "Reservation not found" });

        if (user.Role != "ADMIN" && reservation.UserId != user.Id)
            return Forbid();

        var parkingLot = await _parkingLotAccess.GetById(reservation.ParkingLotId);
        if (parkingLot != null)
        {
            parkingLot.Reserved = Math.Max(0, parkingLot.Reserved - 1);
            await _parkingLotAccess.Update(parkingLot);
        }

        await _reservationAccess.Delete(reservationId);
        return Ok(new { status = "Deleted" });
    }

    [HttpGet("{reservationId}")]
    public async Task<IActionResult> GetReservation(int reservationId)
    {
        var user = GetCurrentUser();
        var reservation = await _reservationAccess.GetById(reservationId);

        if (reservation == null)
            return NotFound(new { error = "Reservation not found" });

        if (user.Role != "ADMIN" && reservation.UserId != user.Id)
            return Forbid();

        return Ok(reservation);
    }
}