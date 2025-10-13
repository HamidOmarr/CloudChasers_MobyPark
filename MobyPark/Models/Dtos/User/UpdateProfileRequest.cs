using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models.Requests.User;

public class UpdateProfileRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }
}
