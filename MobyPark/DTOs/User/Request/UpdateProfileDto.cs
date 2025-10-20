using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.User.Request;

public class UpdateProfileDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public long? RoleId { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    public DateOnly? Birthday { get; set; }
}
