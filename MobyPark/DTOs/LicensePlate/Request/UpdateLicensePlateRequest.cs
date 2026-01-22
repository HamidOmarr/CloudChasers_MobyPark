using System.ComponentModel.DataAnnotations;

using MobyPark.Models.Repositories.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.LicensePlate.Request;

[SwaggerSchema(Description = "Data to update an existing license plate.")]
public class UpdateLicensePlateRequest : ICanBeEdited
{
    [Required]
    [SwaggerSchema("The current license plate number")]
    public string OldLicensePlate { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The new license plate number")]
    public string NewLicensePlate { get; set; } = string.Empty;
}