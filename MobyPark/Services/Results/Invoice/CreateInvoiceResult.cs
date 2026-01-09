using MobyPark.Models;
using MobyPark.DTOs.Invoice;

namespace MobyPark.Services.Results.Invoice;

public abstract record CreateInvoiceResult
{
    public sealed record Success(InvoiceResponseDto Invoice) : CreateInvoiceResult;
    public sealed record ValidationError(string Message) : CreateInvoiceResult;
    public sealed record AlreadyExists : CreateInvoiceResult;
    public sealed record Error(string Message) : CreateInvoiceResult;
}