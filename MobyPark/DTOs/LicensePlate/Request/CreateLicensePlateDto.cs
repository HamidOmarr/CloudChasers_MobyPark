using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.LicensePlate.Request;

[SwaggerSchema(Description = "Data required to register a new license plate.")]
public class CreateLicensePlateDto
{
    [Required]
    [SwaggerSchema("The license plate number (e.g., AA-12-BB)")]
    public string LicensePlate { get; set; } = string.Empty;
}