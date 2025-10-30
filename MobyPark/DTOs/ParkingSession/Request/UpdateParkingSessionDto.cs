using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.ParkingSession.Request;

public class UpdateParkingSessionDto : ICanBeEdited
{
    public DateTime? Stopped { get; set; }
    public ParkingSessionStatus? PaymentStatus { get; set; }
    public decimal? Cost { get; set; }
}