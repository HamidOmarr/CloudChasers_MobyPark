using MobyPark.Models;

namespace MobyPark.Services.Results.Invoice;

public abstract record CreateInvoiceResult
{
    public sealed record Success(InvoiceModel Invoice) : CreateInvoiceResult;
    public sealed record ValidationError(string Message) : CreateInvoiceResult;
    public sealed record AlreadyExists : CreateInvoiceResult;
    public sealed record Error(string Message) : CreateInvoiceResult;
}