using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.User.Response;

[SwaggerSchema(Description = "User profile with associated hotel details.")]
public class UserHotelProfileDto
{
    [SwaggerSchema("The unique identifier of the user.")]
    public long Id { get; set; }
    [SwaggerSchema("The username of the user.")]
    public string Username { get; set; } = string.Empty;
    [SwaggerSchema("The first name of the user.")]
    public string FirstName { get; set; } = string.Empty;
    [SwaggerSchema("The last name of the user.")]
    public string LastName { get; set; } = string.Empty;
    [SwaggerSchema("The email address of the user.")]
    public string Email { get; set; } = string.Empty;
    [SwaggerSchema("The phone number of the user.")]
    public string Phone { get; set; } = string.Empty;
    [SwaggerSchema("The birthday of the user.")]
    public DateTimeOffset Birthday { get; set; } = DateTimeOffset.MinValue;

    [SwaggerSchema("Associated Hotel ID")]
    public long HotelId { get; set; }
    [SwaggerSchema("Associated Hotel Name")]
    public string HotelName { get; set; } = string.Empty;
    [SwaggerSchema("Associated Hotel Address")]
    public string HotelAddress { get; set; } = string.Empty;
    [SwaggerSchema("Associated Hotel IBAN")]
    public string HotelIBAN { get; set; } = string.Empty;
    [SwaggerSchema("Associated Hotel Parking Lot ID")]
    public long HotelParkingLotId { get; set; }
}