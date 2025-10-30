using System.ComponentModel.DataAnnotations;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.Role.Request;

public class UpdateRoleDto : ICanBeEdited
{
    [Required]
    public long Id { get; set; }

    [MaxLength(50)]
    public string? Name { get; set; }

    public string? Description { get; set; }
}