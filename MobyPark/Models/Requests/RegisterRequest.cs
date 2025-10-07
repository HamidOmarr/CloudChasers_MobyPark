using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models.Requests;

public class RegisterRequest : IValidatableObject
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    public DateTime Birthday { get; set; }

    [Required, Phone]
    public string Phone { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Passwords do not match",
                new[] { nameof(Password), nameof(ConfirmPassword) }
            );
        }
    }
}