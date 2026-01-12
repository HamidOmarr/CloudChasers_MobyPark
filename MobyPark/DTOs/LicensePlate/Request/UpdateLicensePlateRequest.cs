using System.ComponentModel.DataAnnotations;

using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.LicensePlate.Request;

public class UpdateLicensePlateRequest : ICanBeEdited
{
    [Required]
    public string OldLicensePlate { get; set; } = string.Empty;

    [Required]
    public string NewLicensePlate { get; set; } = string.Empty;
}