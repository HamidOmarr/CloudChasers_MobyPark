using MobyPark.Models.Repositories.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Token;

[SwaggerSchema(Description = "Details regarding authentication refresh tokens.")]
public class TokenDto : ICanBeEdited
{
    [SwaggerSchema("The refresh token string.")]
    public string RefreshToken { get; set; } = string.Empty;

    [SwaggerSchema("The expiration time for the sliding window validity.")]
    public DateTimeOffset SlidingTokenExpiryTime { get; set; }

    [SwaggerSchema("The absolute hard expiration time for the token.")]
    public DateTimeOffset AbsoluteTokenExpiryTime { get; set; }
}