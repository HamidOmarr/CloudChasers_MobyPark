using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models;

public class UserModel : IHasLongId, ICanBeEdited
{
    [Key]
    public long Id { get; set; }

    [MaxLength(32), Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = null!;

    [MaxLength(50), Required]
    public string FirstName { get; set; } = null!;

    [MaxLength(50), Required]
    public string LastName { get; set; } = null!;

    [MaxLength(320), Required]
    public string Email { get; set; } = null!;

    [MaxLength(20), Required]
    public string Phone { get; set; } = null!;

    [Required] public long RoleId { get; set; } = DefaultUserRoleId;

    [ForeignKey(nameof(RoleId))]
    public RoleModel Role { get; set; } = null!;

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset Birthday { get; set; } = DateTimeOffset.MinValue;  // TODO: Set up default value, update later

    public long? HotelId { get; set; } = null;
    
    [ForeignKey(nameof(HotelId))]
    public HotelModel? Hotel { get; set; }

    public long? BusinessId { get; set; } = null;
    [ForeignKey(nameof(BusinessId))]
    public BusinessModel? Business { get; set; }
    
    public const long AdminRoleId = 1;
    public const long DefaultUserRoleId = 6;  // Defaults to 'User' role
}
