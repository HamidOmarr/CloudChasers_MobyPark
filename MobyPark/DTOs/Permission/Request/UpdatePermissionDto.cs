using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.Permission.Request;

public class UpdatePermissionDto
{
    [Required]
    [StringLength(100)]
    public string Resource { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;
}