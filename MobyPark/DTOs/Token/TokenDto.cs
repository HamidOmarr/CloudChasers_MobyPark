using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.Token;

public class TokenDto : ICanBeEdited
{
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset SlidingTokenExpiryTime { get; set; }
    public DateTimeOffset AbsoluteTokenExpiryTime { get; set; }
}