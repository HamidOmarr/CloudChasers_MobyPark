using MobyPark.Models;

namespace MobyPark.Services.Results.Invoice;

public abstract record GetInvoiceResult
{
    public sealed record Success(InvoiceModel Invoice) : GetInvoiceResult;
    public sealed record NotFound : GetInvoiceResult;
    public sealed record Error(string Message) : GetInvoiceResult;
}