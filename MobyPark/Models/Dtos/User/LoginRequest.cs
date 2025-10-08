using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models.Requests.User;

public class LoginRequest
{
    [Required]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
