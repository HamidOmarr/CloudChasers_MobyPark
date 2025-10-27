using System.ComponentModel.DataAnnotations;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class PermissionModel : IHasLongId
{
    [Key]
    public long Id { get; set; }

    [MaxLength(100), Required]
    public string Resource { get; set; } = string.Empty;

    [MaxLength(50), Required]
    public string Action { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Key { get; set; }
    public ICollection<RolePermissionModel> RolePermissions { get; set; } = new List<RolePermissionModel>();
}
