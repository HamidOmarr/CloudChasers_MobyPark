using MobyPark.DTOs.Invoice;

namespace MobyPark.Services.Results.Invoice;

public abstract record GetInvoiceResult
{
    public sealed record Success(InvoiceResponseDto Invoice) : GetInvoiceResult;
    public sealed record NotFound : GetInvoiceResult;
    public sealed record Error(string Message) : GetInvoiceResult;
}