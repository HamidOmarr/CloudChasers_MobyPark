namespace MobyPark.Models.Responses.User;

public class AdminUserProfileResponse : UserProfileResponse
{
    public string Role { get; set; } = "USER";
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}
