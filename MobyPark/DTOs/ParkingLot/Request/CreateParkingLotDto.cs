using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.ParkingLot.Request;

[SwaggerSchema(Description = "Data required to create a new parking lot.")]
public class CreateParkingLotDto
{
    [Required]
    [SwaggerSchema("The display name of the parking lot")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The general location or city (e.g. 'Downtown')")]
    public string Location { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The full street address")]
    public string Address { get; set; } = string.Empty;

    [Required, Range(1, 20000)]
    [SwaggerSchema("The total number of parking spots available")]
    public int Capacity { get; set; } = 200;

    [Required, Range(0, 100)]
    [SwaggerSchema("The standard hourly tariff")]
    public decimal Tariff { get; set; } = 1.0m;

    [SwaggerSchema("Optional discounted tariff for full-day parking")]
    public decimal? DayTariff { get; set; }
}