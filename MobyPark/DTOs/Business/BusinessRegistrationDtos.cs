using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Business;

[SwaggerSchema(Description = "Details of a business parking registration.")]
public class ReadBusinessRegDto
{
    [SwaggerSchema("The unique identifier of the registration")]
    public long Id { get; set; }

    [SwaggerSchema("The ID of the associated business")]
    public long BusinessId { get; set; }

    [SwaggerSchema("The license plate number")]
    public string LicensePlateNumber { get; set; } = string.Empty;

    [SwaggerSchema("Whether the registration is currently valid")]
    public bool Active { get; set; }

    [SwaggerSchema("The timestamp when the active status was last changed")]
    public DateTimeOffset LastSinceActive { get; set; }
}

[SwaggerSchema(Description = "Data for an admin to create a new registration.")]
public class CreateBusinessRegAdminDto
{
    [Required]
    [SwaggerSchema("The ID of the business to link this registration to")]
    public long BusinessId { get; set; }

    [Required]
    [SwaggerSchema("The license plate number")]
    public string LicensePlateNumber { get; set; } = string.Empty;

    [SwaggerSchema("Initial active status (default: false)")]
    public bool Active { get; set; }
}

[SwaggerSchema(Description = "Data for a user to register their own vehicle.")]
public class CreateBusinessRegDto
{
    [Required]
    [SwaggerSchema("The license plate number")]
    public string LicensePlateNumber { get; set; } = string.Empty;

    [SwaggerSchema("Initial active status")]
    public bool Active { get; set; }
}

[SwaggerSchema(Description = "Data to update the status of an existing registration.")]
public class PatchBusinessRegDto
{
    [Required]
    [SwaggerSchema("The unique identifier of the registration to update")]
    public long Id { get; set; }

    [SwaggerSchema("The new active status")]
    public bool Active { get; set; }
}