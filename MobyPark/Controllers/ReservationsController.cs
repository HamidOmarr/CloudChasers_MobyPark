using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Models.Requests;
using MobyPark.Services.Services;

namespace MobyPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : BaseController
{
    private readonly ServiceStack _services;

    public ReservationsController(ServiceStack services) : base(services.Sessions)
    {
        _services = services;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReservationRequest request)
    {
        var user = GetCurrentUser();

        if (request.StartDate == default || request.EndDate == default)
            return BadRequest(new { error = "Required fields missing" });

        var parkingLot = await _services.ParkingLots.GetParkingLotById(request.ParkingLotId);
        var vehicle = await _services.Vehicles.GetVehicleByLicensePlate(request.LicensePlate);

        if (parkingLot is null)
            return NotFound(new { error = "Parking lot not found" });

        UserModel? userRequested = null;

        if (request.Username is not null)
            userRequested = await _services.Users.GetUserByUsername(request.Username);

        int userId = user.Role == "ADMIN" && userRequested is not null ? userRequested.Id : user.Id; // Ensures that if an admin makes a manual reservation, the admin user is not set as the user in reservation.

        int cost;
        if (request.EndDate - request.StartDate > TimeSpan.FromHours(24))
        {
            var days = (int)((request.EndDate - request.StartDate).TotalDays);
            if ((request.EndDate - request.StartDate).TotalHours % 24 > 0)
                days += 1;
            cost = (int)(days * parkingLot.DayTariff);
        }
        else
            cost = (int)((request.EndDate - request.StartDate).TotalHours * (double)parkingLot.Tariff);

        var reservation = new ReservationModel
        {
            UserId = userId,
            ParkingLotId = request.ParkingLotId,
            VehicleId = vehicle.Id,
            StartTime = request.StartDate,
            EndTime = request.EndDate,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            Cost = cost
        };

        var reservationId = await _services.Reservations.CreateReservation(reservation);
        reservation.Id = reservationId;

        parkingLot.Reserved += 1;

        return StatusCode(201, new { status = "Success", reservation });
    }

    [HttpPut("{reservationId}")]
    public async Task<IActionResult> UpdateReservation(int reservationId, [FromBody] ReservationModel request)
    {
        var user = GetCurrentUser();
        ReservationModel reservation = await _services.Reservations.GetReservationById(reservationId);

        if (request.StartTime == default || request.EndTime == default || request.ParkingLotId == 0)
            return BadRequest(new { error = "Required fields missing" });

        if (user.Role != "ADMIN")
            request.UserId = user.Id;

        await _services.Reservations.UpdateReservation(request.ParkingLotId, request.VehicleId, request.StartTime, request.EndTime, request.UserId);
        return Ok(new { status = "Updated", reservation });
    }

    [HttpDelete("{reservationId}")]
    public async Task<IActionResult> DeleteReservation(int reservationId)
    {
        var user = GetCurrentUser();
        ReservationModel reservation = await _services.Reservations.GetReservationById(reservationId);

        if (user.Role != "ADMIN" && reservation.UserId != user.Id)
            return Forbid();

        var parkingLot = await _services.ParkingLots.GetParkingLotById(reservation.ParkingLotId);
        parkingLot.Reserved = Math.Max(0, parkingLot.Reserved - 1);

        await _services.ParkingLots.UpdateParkingLot(parkingLot.Id, parkingLot.Name, parkingLot.Location,
            parkingLot.Address, parkingLot.Capacity, parkingLot.Reserved, parkingLot.Tariff, parkingLot.DayTariff,
            parkingLot.CreatedAt, parkingLot.Coordinates);

        bool success = await _services.Reservations.DeleteReservation(reservationId);
        return success
            ? Ok(new { status = "Deleted" })
            : StatusCode(500, new { status = "Error", message = "Failed to delete reservation" });
    }

    [HttpGet("{reservationId}")]
    public async Task<IActionResult> GetReservation(int reservationId)
    {
        var user = GetCurrentUser();
        ReservationModel reservation = await _services.Reservations.GetReservationById(reservationId);

        if (user.Role != "ADMIN" && reservation.UserId != user.Id)
            return Forbid();

        return Ok(reservation);
    }
}