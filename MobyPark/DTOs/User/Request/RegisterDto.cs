using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.User.Request;

[SwaggerSchema(Description = "Data required to register a new user account.")]
public class RegisterDto
{
    [Required, EmailAddress]
    [SwaggerSchema("A valid email address")]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(3)]
    [SwaggerSchema("A unique username")]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    [SwaggerSchema("A strong password (min 8 chars, 1 uppercase, 1 number)")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    [SwaggerSchema("Confirmation of the password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("Date of birth (User must be 16+)")]
    public DateTimeOffset Birthday { get; set; }

    [Required, Phone]
    [SwaggerSchema("Valid Dutch phone number")]
    public string Phone { get; set; } = string.Empty;

    [SwaggerSchema("Optional initial license plate to register")]
    public string? LicensePlate { get; set; }
}