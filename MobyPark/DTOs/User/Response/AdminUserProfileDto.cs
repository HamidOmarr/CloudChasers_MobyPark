namespace MobyPark.DTOs.User.Response;

public class AdminUserProfileDto : UserProfileDto
{
    public string Role { get; set; } = "USER";
    public DateTimeOffset CreatedAt { get; set; }
}