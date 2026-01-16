using MobyPark.Models;

namespace MobyPark.Services.Results.Invoice;

public abstract record DeleteInvoiceResult
{
    public sealed record Success : DeleteInvoiceResult;
    public sealed record NotFound() : DeleteInvoiceResult;
    public sealed record Error(string Message) : DeleteInvoiceResult;
}