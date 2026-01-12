using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.DTOs.Transaction.Request;

public class TransactionDataDto : ICanBeEdited
{
    public string Method { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Bank { get; set; } = string.Empty;
}