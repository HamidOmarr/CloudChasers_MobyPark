namespace MobyPark.DTOs.User.Response;

public class AdminUserProfileDto : UserProfileDto
{
    public string Role { get; set; } = "USER";
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}
