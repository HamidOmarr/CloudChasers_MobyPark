using System.ComponentModel.DataAnnotations;

namespace MobyPark.Models;

public class LicensePlateModel
{
    [Key, Required]
    public string LicensePlateNumber { get; set; } = string.Empty;
}
