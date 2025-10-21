using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.LicensePlate.Request;

public class UpdateLicensePlateRequest
{
    [Required]
    public string OldLicensePlate { get; set; } = string.Empty;

    [Required]
    public string NewLicensePlate { get; set; } = string.Empty;
}