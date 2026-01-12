using System.ComponentModel.DataAnnotations;

using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.ParkingLot.Request;

public class PatchParkingLotDto
{
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? Address { get; set; }

    [Range(1, 20000)]
    public int? Capacity { get; set; }

    [Range(0, 100)]
    public decimal? Tariff { get; set; }

    public decimal? DayTariff { get; set; }

    [Range(0, 20000)]
    public int? Reserved { get; set; }
}