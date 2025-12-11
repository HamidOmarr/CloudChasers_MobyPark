namespace MobyPark.DTOs.User.Response;

public class UserBusinessProfileDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTimeOffset Birthday { get; set; } = DateTimeOffset.MinValue;
    public long BusinessId { get; set; }
    public string BusinessName { get; set; }
    public string BusinessAddress { get; set; }
    public string BusinessIBAN { get; set; }
}