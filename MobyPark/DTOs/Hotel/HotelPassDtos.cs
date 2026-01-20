using System.ComponentModel.DataAnnotations;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Hotel;

[SwaggerSchema(Description = "Data for a hotelier to create a pass (Lot ID inferred from user).")]
public class CreateHotelPassDto
{
    [Required]
    [SwaggerSchema("License plate of the guest")]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("Start time of the reservation (UTC)")]
    public DateTime Start { get; set; }

    [Required]
    [SwaggerSchema("End time of the reservation (UTC)")]
    public DateTime End { get; set; }

    [SwaggerSchema("Grace period added to the end time (Default: 30 mins)")]
    public TimeSpan ExtraTime { get; set; } = new(0, 30, 0);
}

[SwaggerSchema(Description = "Data for an admin to create a pass (Must specify Lot ID).")]
public class AdminCreateHotelPassDto
{
    [Required]
    [SwaggerSchema("License plate of the guest")]
    public string LicensePlate { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The ID of the parking lot")]
    public long ParkingLotId { get; set; }

    [Required]
    [SwaggerSchema("Start time of the reservation (UTC)")]
    public DateTime Start { get; set; }

    [Required]
    [SwaggerSchema("End time of the reservation (UTC)")]
    public DateTime End { get; set; }

    [SwaggerSchema("Grace period (Default: 30 mins)")]
    public TimeSpan ExtraTime { get; set; } = new(0, 30, 0);
}

[SwaggerSchema(Description = "Details of a Hotel Pass.")]
public class ReadHotelPassDto
{
    [Required]
    [SwaggerSchema("Unique pass ID")]
    public long Id { get; set; }

    [Required]
    [SwaggerSchema("Guest license plate")]
    public string LicensePlate { get; set; } = string.Empty;
    [Required]
    public long ParkingLotId { get; set; }
    [Required]
    public DateTime Start { get; set; }
    [Required]
    public DateTime End { get; set; }
    [Required]
    public TimeSpan ExtraTime { get; set; }
}

[SwaggerSchema(Description = "Data to update a hotel pass.")]
public class PatchHotelPassDto
{
    [Required]
    [SwaggerSchema("Unique pass ID")]
    public long Id { get; set; }
    public string? LicensePlate { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public TimeSpan? ExtraTime { get; set; }
}