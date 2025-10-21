using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.ParkingLot.Request;

public class ParkingLotUpdateDto
{
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? Address { get; set; }

    [Range(1, 20000)]
    public int? Capacity { get; set; } = 200;

    [Range(0, 100)]
    public decimal? Tariff { get; set; } = 1.0m;

    public decimal? DayTariff { get; set; } = null;
}