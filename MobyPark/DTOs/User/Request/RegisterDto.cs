using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.User.Request;

public class RegisterDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    public DateOnly Birthday { get; set; }

    [Required, Phone]
    public string Phone { get; set; } = string.Empty;
}
