using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.User.Response;

[SwaggerSchema(Description = "User profile with associated business details.")]
public class UserBusinessProfileDto
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

    [SwaggerSchema("Associated Business ID")]
    public long BusinessId { get; set; }
    [SwaggerSchema("Associated Business Name")]
    public string BusinessName { get; set; } = string.Empty;
    [SwaggerSchema("Associated Business Address")]
    public string BusinessAddress { get; set; } = string.Empty;
    [SwaggerSchema("Associated Business IBAN")]
    public string BusinessIBAN { get; set; } = string.Empty;
}