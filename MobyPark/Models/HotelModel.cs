namespace MobyPark.Models;

public class HotelModel
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string IBAN { get; set; }
    public long HotelParkingLotId { get; set; }
}

//create hotel model