using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.PreAuth.Response;

[SwaggerSchema(Description = "Result of a payment pre-authorization attempt.")]
public class PreAuthDto
{
    [SwaggerSchema("Indicates if the pre-authorization was successful.")]
    public bool Approved { get; set; }

    [SwaggerSchema("The reason for failure, if applicable (e.g., 'Insufficient Funds').")]
    public string? Reason { get; set; }
}