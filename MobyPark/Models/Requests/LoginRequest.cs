using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models.Requests;

public class LoginRequest
{
    [Required] public string Identifier { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}