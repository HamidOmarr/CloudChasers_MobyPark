using System.ComponentModel.DataAnnotations;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.User.Request;

public class UpdateUserIdentityDto : ICanBeEdited
{
    [StringLength(100, MinimumLength = 1)]
    public string? FirstName { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? LastName { get; set; }

    public DateOnly? Birthday { get; set; }
}
