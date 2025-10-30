namespace MobyPark.DTOs.User.Response;

public class UserProfileDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateOnly Birthday { get; set; } = DateOnly.MinValue;
}
