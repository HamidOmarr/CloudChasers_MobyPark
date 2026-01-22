using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Cards;

[SwaggerSchema(Description = "Data Transfer Object for creating a new card information")]
public class CreateCardInfoDto
{
    [SwaggerSchema("The token associated with the card")]
    public string Token { get; set; } = string.Empty;
    [SwaggerSchema("The method of the card (e.g., credit, debit)")]
    public string Method { get; set; } = string.Empty;
    [SwaggerSchema("The bank associated with the card")]
    public string Bank { get; set; } = string.Empty;
    [SwaggerSchema("The available funds on the card")]
    public decimal? AvailableFunds { get; set; }
}