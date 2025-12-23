using System.Security.Cryptography;
using System.Text;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.DTOs.Invoice;
using MobyPark.Services.Results;
using MobyPark.Validation;
using MobyPark.Services.Results.Invoice;

namespace MobyPark.Services;

public class AutomatedInvoiceService : IAutomatedInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    public AutomatedInvoiceService(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<CreateInvoiceResult> CreateInvoice(CreateInvoiceDto invoiceDto)
    {
        try
        {
            var existingInvoice = await _invoiceRepository.GetInvoiceModelByLicensePlate(invoiceDto.LicensePlateId);
            if (existingInvoice != null)
            {
                return new CreateInvoiceResult.AlreadyExists();
            }

            var invoice = new InvoiceModel
            {
                LicensePlateId = invoiceDto.LicensePlateId,
                ParkingSessionId = invoiceDto.ParkingSessionId,
                Started = invoiceDto.Started,
                Stopped = invoiceDto.Stopped,
                Cost = invoiceDto.Cost,
                InvoiceSummary = invoiceDto.InvoiceSummary
            };

            await _invoiceRepository.Create(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return new CreateInvoiceResult.Success(invoice);
        }
        catch (Exception ex)
        {
            return new CreateInvoiceResult.Error($"An error occurred while creating the invoice: {ex.Message}");
        }
    }

    public async Task<GetInvoiceResult> GetInvoiceByLicensePlate(string licensPLateId)
    {
        try
        {
            var invoice = await _invoiceRepository.GetInvoiceModelByLicensePlate(licensPLateId);
            if (invoice == null)
            {
                return new GetInvoiceResult.NotFound();
            }

            return new GetInvoiceResult.Success(invoice);
        }
        catch (Exception ex)
        {
            return new GetInvoiceResult.Error($"An error occurred while retrieving the invoice: {ex.Message}");
        }
    }

    public async Task<UpdateInvoiceResult> UpdateInvoice(string LicensePlateId, UpdateInvoiceDto updateDto)
    {
        try
        {
            var invoice = await _invoiceRepository.GetInvoiceModelByLicensePlate(LicensePlateId);
            if (invoice == null)
            {
                return new UpdateInvoiceResult.NotFound();
            }

            invoice.Started = updateDto.Started;
            invoice.Stopped = updateDto.Stopped;
            invoice.Cost = updateDto.Cost;
            invoice.InvoiceSummary = updateDto.InvoiceSummary;
            invoice.Status = updateDto.Status;



            _invoiceRepository.Update(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return new UpdateInvoiceResult.Success(invoice);
        }
        catch (Exception ex)
        {
            return new UpdateInvoiceResult.Error($"An error occurred while updating the invoice: {ex.Message}");
        }
    }


}