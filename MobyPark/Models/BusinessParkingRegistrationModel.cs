using System.ComponentModel.DataAnnotations.Schema;

using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class BusinessParkingRegistrationModel : IHasLongId
{
    public long Id { get; set; }
    public long BusinessId { get; set; }

    [ForeignKey(nameof(BusinessId))]
    public BusinessModel Business { get; set; } = null!;

    public string LicensePlateNumber { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTimeOffset LastSinceActive { get; set; } = DateTimeOffset.Now;
}