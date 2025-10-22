using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.LicensePlate.Request;

public class CreateLicensePlateDto
{
    [Required]
    public string LicensePlate { get; set; } = string.Empty;
}
