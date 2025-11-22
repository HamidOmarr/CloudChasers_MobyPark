using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class HotelPassModel : IHasLongId
{ 
    public long Id { get; }
    public int ParkingLotId { get; set; }
    public string LicensePlate { get; set; }
    public ParkingLotModel ParkingLot { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    
    public TimeSpan ExtraTime { get; set; }
}