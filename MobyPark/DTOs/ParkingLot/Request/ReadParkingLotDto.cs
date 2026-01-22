using MobyPark.Models;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.ParkingLot.Request;

[SwaggerSchema(Description = "Details of a registered parking lot.")]
public class ReadParkingLotDto
{
    [SwaggerSchema("The unique identifier of the parking lot")]
    public long Id { get; set; }

    [SwaggerSchema("The display name")]
    public string Name { get; set; } = string.Empty;

    [SwaggerSchema("The general location")]
    public string Location { get; set; } = string.Empty;

    [SwaggerSchema("The full address")]
    public string Address { get; set; } = string.Empty;

    [SwaggerSchema("Number of reserved spots")]
    public int Reserved { get; set; }

    [SwaggerSchema("Total capacity")]
    public int Capacity { get; set; }

    [SwaggerSchema("Hourly tariff")]
    public decimal Tariff { get; set; }

    [SwaggerSchema("Daily tariff (if applicable)")]
    public decimal? DayTariff { get; set; }

    [SwaggerSchema("Date of creation")]
    public DateTimeOffset CreatedAt { get; set; }

    [SwaggerSchema("Current operational status")]
    public ParkingLotStatus Status { get; set; }

    [SwaggerSchema("Associated reservations")]
    public ICollection<ReservationModel> Reservations { get; set; } = new List<ReservationModel>();
}