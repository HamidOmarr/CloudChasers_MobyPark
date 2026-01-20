using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.ParkingLot.Request;

[SwaggerSchema(Description = "Data for updating specific fields of an existing parking lot.")]
public class PatchParkingLotDto
{
    [SwaggerSchema("The new display name")]
    public string? Name { get; set; }

    [SwaggerSchema("The new general location")]
    public string? Location { get; set; }

    [SwaggerSchema("The new street address (must be unique)")]
    public string? Address { get; set; }

    [Range(1, 20000)]
    [SwaggerSchema("The new total capacity")]
    public int? Capacity { get; set; }

    [Range(0, 100)]
    [SwaggerSchema("The new hourly tariff")]
    public decimal? Tariff { get; set; }

    [SwaggerSchema("The new day tariff")]
    public decimal? DayTariff { get; set; }

    [Range(0, 20000)]
    [SwaggerSchema("The number of spots reserved for specific uses")]
    public int? Reserved { get; set; }
}