using System.ComponentModel.DataAnnotations;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class RoleModel : IHasLongId
{
    [Key]
    public long Id { get; set; }

    [MaxLength(50), Required]
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;
    public ICollection<UserModel> Users { get; set; } = new List<UserModel>();
    public ICollection<RolePermissionModel> RolePermissions { get; set; } = new List<RolePermissionModel>();
}
