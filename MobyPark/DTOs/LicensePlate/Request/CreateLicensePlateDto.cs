using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.LicensePlate;

public class CreateLicensePlateDto
{
    [Required]
    public string LicensePlate { get; set; } = string.Empty;
}
