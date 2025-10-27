using System.ComponentModel.DataAnnotations;

namespace MobyPark.DTOs.Role.Request;

public class CreateRoleDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}