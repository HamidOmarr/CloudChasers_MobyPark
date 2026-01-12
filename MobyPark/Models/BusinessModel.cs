namespace MobyPark.Models;

public class BusinessModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
}