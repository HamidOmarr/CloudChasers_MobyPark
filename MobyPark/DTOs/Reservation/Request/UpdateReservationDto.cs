using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.Reservation.Request;

[SwaggerSchema(Description = "Data required to update an existing reservation.")]
public class UpdateReservationDto : ICanBeEdited
{
    [SwaggerSchema("The new start time (cannot be changed if already started).")]
    public DateTimeOffset? StartTime { get; set; }

    [SwaggerSchema("The new end time.")]
    public DateTimeOffset? EndTime { get; set; }

    [SwaggerSchema("The new status (e.g., Cancelled).")]
    public ReservationStatus? Status { get; set; }  // Allow updating the status, to set in to Cancelled or NoShow if needed
}