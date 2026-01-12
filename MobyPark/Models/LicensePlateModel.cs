using System.ComponentModel.DataAnnotations;

using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class LicensePlateModel : ICanBeEdited
{
    [Key, Required]
    public string LicensePlateNumber { get; set; } = string.Empty;
}