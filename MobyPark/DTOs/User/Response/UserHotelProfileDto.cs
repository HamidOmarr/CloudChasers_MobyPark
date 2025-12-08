namespace MobyPark.DTOs.User.Response;

public class UserHotelProfileDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTimeOffset Birthday { get; set; } = DateTimeOffset.MinValue;
    public long HotelId { get; set; }
    public string HotelName { get; set; }
    public string HotelAddress { get; set; }
    public string HotelIBAN { get; set; }
    public long HotelParkingLotId { get; set; }
}