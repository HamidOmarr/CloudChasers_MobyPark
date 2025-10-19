using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models.Requests.User;

public class ConvertGuestRequest
{
    [Required]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone]
    public string Phone { get; set; } = string.Empty;
}
