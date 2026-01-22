using System.ComponentModel.DataAnnotations;

using MobyPark.Models.Repositories.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.User.Request;

[SwaggerSchema(Description = "Admin-only data for updating user identity.")]
public class UpdateUserIdentityDto : ICanBeEdited
{
    [StringLength(100, MinimumLength = 1)]
    [SwaggerSchema(Description = "The new first name for the user (optional).")]
    public string? FirstName { get; set; }

    [SwaggerSchema(Description = "The new last name for the user (optional).")]
    [StringLength(100, MinimumLength = 1)]
    public string? LastName { get; set; }

    [SwaggerSchema(Description = "The new birthday for the user (optional).")]
    public DateTimeOffset? Birthday { get; set; }
}