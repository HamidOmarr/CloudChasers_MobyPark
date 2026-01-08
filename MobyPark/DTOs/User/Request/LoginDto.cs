using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.User.Request;

public class LoginDto
{
    [Required]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}