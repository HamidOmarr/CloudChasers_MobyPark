using System.ComponentModel.DataAnnotations;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.Reservation.Request;

public class UpdateReservationDto : ICanBeEdited
{
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public ReservationStatus? Status { get; set; }  // Allow updating the status, to set in to Cancelled or NoShow if needed
}
