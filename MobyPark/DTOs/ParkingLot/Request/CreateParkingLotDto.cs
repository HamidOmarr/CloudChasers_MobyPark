using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.ParkingLot.Request;

public class CreateParkingLotDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required, Range(1, 20000)]
    public int Capacity { get; set; } = 200;

    [Required, Range(0, 100)]
    public decimal Tariff { get; set; } = 1.0m;

    public decimal? DayTariff { get; set; }
}