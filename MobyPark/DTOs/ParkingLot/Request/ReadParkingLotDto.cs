using MobyPark.Models;

namespace MobyPark.DTOs.ParkingLot.Request;

public class ReadParkingLotDto
{
    public long Id { get; set; } // Always return ID
    public string Name { get; set; }
    public string Location { get; set; }
    public string Address { get; set; }
    public int Reserved { get; set; }
    public int Capacity { get; set; }
    public decimal Tariff { get; set; }
    public decimal? DayTariff { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ParkingLotStatus Status { get; set; }
    public ICollection<ReservationModel> Reservations { get; set; }
}