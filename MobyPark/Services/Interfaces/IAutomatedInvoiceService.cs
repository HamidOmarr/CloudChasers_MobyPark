using MobyPark.DTOs.Invoice;
using MobyPark.Services.Results.Invoice;

namespace MobyPark.Services.Interfaces;

public interface IAutomatedInvoiceService
{
    Task<CreateInvoiceResult> CreateInvoice(CreateInvoiceDto invoiceDto);
    Task<GetInvoiceResult> GetInvoiceByLicensePlate(string licensePlateId);
    Task<UpdateInvoiceResult> UpdateInvoice(string LicensePlateId, UpdateInvoiceDto updateDto);
}