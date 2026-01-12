using System.ComponentModel.DataAnnotations.Schema;

namespace MobyPark.Models;

public class HotelModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public long HotelParkingLotId { get; set; }

    [ForeignKey(nameof(HotelParkingLotId))]
    public ParkingLotModel ParkingLot { get; set; } = null!;
}

//create hotel model