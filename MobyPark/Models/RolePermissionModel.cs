using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class RolePermissionModel : ICanBeEdited
{
    [Key]
    public long RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public RoleModel Role { get; set; } = null!;

    [Key]
    public long PermissionId { get; set; }

    [ForeignKey(nameof(PermissionId))]
    public PermissionModel Permission { get; set; } = null!;
}
