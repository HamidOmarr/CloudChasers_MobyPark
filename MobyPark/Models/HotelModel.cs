using System.ComponentModel.DataAnnotations.Schema;

namespace MobyPark.Models;

public class HotelModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string IBAN { get; set; }
    public long HotelParkingLotId { get; set; }
    
    [ForeignKey(nameof(HotelParkingLotId))]
    public ParkingLotModel ParkingLot { get; set; }
}

//create hotel model