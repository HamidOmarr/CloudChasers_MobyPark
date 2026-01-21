using MobyPark.Models;


namespace MobyPark.Services.Results.Invoice;

public abstract record UpdateInvoiceResult
{
    public sealed record Success(InvoiceModel Invoice) : UpdateInvoiceResult;
    public sealed record NotFound() : UpdateInvoiceResult;
    public sealed record Error(string Message) : UpdateInvoiceResult;
}