using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Business;

[SwaggerSchema(Description = "Data Transfer Object for reading business information")]
public class ReadBusinessDto
{
    [SwaggerSchema("The unique identifier of the business")]
    public long Id { get; set; }

    [SwaggerSchema("The official registered name of the business")]
    public string Name { get; set; } = string.Empty;

    [SwaggerSchema("The physical visiting address")]
    public string Address { get; set; } = string.Empty;

    [SwaggerSchema("The bank account number (IBAN format)")]
    public string IBAN { get; set; } = string.Empty;
}

[SwaggerSchema(Description = "Data Transfer Object for creating a new business")]
public class CreateBusinessDto
{
    [Required]
    [MaxLength(100)]
    [SwaggerSchema("The official registered name of the business")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The physical visiting address")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The bank account number (IBAN format)")]
    public string IBAN { get; set; } = string.Empty;
}

[SwaggerSchema(Description = "Data Transfer Object for updating existing business information")]
public class PatchBusinessDto
{
    [SwaggerSchema("The official registered name of the business")]
    public string? Name { get; set; }

    [SwaggerSchema("The physical visiting address")]
    public string? Address { get; set; }

    [SwaggerSchema("The bank account number (IBAN format)")]
    public string? IBAN { get; set; }
}