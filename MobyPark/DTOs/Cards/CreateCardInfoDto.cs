namespace MobyPark.DTOs.Cards;

public class CreateCardInfoDto
{
    public string Token { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Bank { get; set; } = string.Empty;
    public decimal? AvailableFunds { get; set; }
}