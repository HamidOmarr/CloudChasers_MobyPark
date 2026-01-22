using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Shared;

[SwaggerSchema(Description = "Standard error response structure.")]
public class ErrorResponseDto
{
    [SwaggerSchema("The error message describing what went wrong.")]
    public string Error { get; set; } = string.Empty;

    [SwaggerSchema("Optional additional data related to the error.")]
    public object? Data { get; set; }
}