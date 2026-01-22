using System.ComponentModel.DataAnnotations;

using MobyPark.Models.Repositories.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.User.Request;

[SwaggerSchema(Description = "Data for updating user profile details.")]
public class UpdateUserDto : ICanBeEdited
{
    [SwaggerSchema("The new username for the user (optional).")]
    public string? Username { get; set; }
    [SwaggerSchema("The new password for the user (optional).")]
    public string? Password { get; set; }
    [SwaggerSchema("The new first name for the user (optional).")]
    public string? FirstName { get; set; }
    [SwaggerSchema("The new last name for the user (optional).")]
    public string? LastName { get; set; }
    [SwaggerSchema("The new role ID for the user (optional).")]
    public long? RoleId { get; set; }

    [EmailAddress]
    [SwaggerSchema("The new email address for the user (optional).")]
    public string? Email { get; set; }

    [Phone]
    [SwaggerSchema("The new phone number for the user (optional).")]
    public string? Phone { get; set; }

    [SwaggerSchema("The new birthday for the user (optional).")]
    public DateTimeOffset? Birthday { get; set; }
}