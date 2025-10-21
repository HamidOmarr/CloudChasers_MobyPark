using System.ComponentModel.DataAnnotations;
using MobyPark.Models;

namespace MobyPark.DTOs.Reservation.Request;

public class UpdateReservationDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public ReservationStatus? Status { get; set; }  // Allow updating the status, to set in to Cancelled or NoShow if needed
}
