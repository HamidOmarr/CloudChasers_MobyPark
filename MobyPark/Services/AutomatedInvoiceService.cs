using MobyPark.DTOs.Invoice;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
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
            if (invoiceDto.SessionDuration < 0)
            {
                return new CreateInvoiceResult.Error("Session duration can not be negative.");
            }


            var existingInvoice =
                await _invoiceRepository.GetInvoiceModelByLicensePlate(invoiceDto.LicensePlateId);

            if (existingInvoice != null)
            {
                return new CreateInvoiceResult.AlreadyExists();
            }

            var invoice = new InvoiceModel
            {
                LicensePlateId = invoiceDto.LicensePlateId,
                ParkingSessionId = invoiceDto.ParkingSessionId,
                SessionDuration = invoiceDto.SessionDuration,
                Cost = invoiceDto.Cost,
                Status = invoiceDto.Status,
                CreatedAt = DateTimeOffset.UtcNow,
                InvoiceSummary = GenerateInvoiceSummary(invoiceDto)
            };

            await _invoiceRepository.Create(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return new CreateInvoiceResult.Success(invoice);
        }
        catch (Exception ex)
        {
            return new CreateInvoiceResult.Error(
                $"An error occurred while creating the invoice: {ex.Message}");
        }
    }

    public async Task<GetInvoiceResult> GetInvoiceByLicensePlate(string licensePlateId)
    {
        try
        {
            var invoice =
                await _invoiceRepository.GetInvoiceModelByLicensePlate(licensePlateId);

            if (invoice == null)
            {
                return new GetInvoiceResult.NotFound();
            }


            return new GetInvoiceResult.Success(invoice);
        }
        catch (Exception ex)
        {
            return new GetInvoiceResult.Error(
                $"An error occurred while retrieving the invoice: {ex.Message}");
        }
    }

    public async Task<UpdateInvoiceResult> UpdateInvoice(
        string licensePlateId,
        UpdateInvoiceDto updateDto)
    {
        try
        {
            if (updateDto.SessionDuration < 0)
            {
                return new UpdateInvoiceResult.Error(
                    "Session duration can not be negative");
            }

            var invoice =
                await _invoiceRepository.GetInvoiceModelByLicensePlate(licensePlateId);

            if (invoice == null)
            {
                return new UpdateInvoiceResult.NotFound();
            }

            invoice.SessionDuration = updateDto.SessionDuration;
            invoice.Cost = updateDto.Cost;
            invoice.Status = updateDto.Status;
            invoice.InvoiceSummary = GenerateInvoiceSummary(invoice);

            _invoiceRepository.Update(invoice);
            await _invoiceRepository.SaveChangesAsync();


            return new UpdateInvoiceResult.Success(invoice);
        }
        catch (Exception ex)
        {
            return new UpdateInvoiceResult.Error(
                $"An error occurred while updating the invoice: {ex.Message}");
        }
    }

    private static List<string> GenerateInvoiceSummary(CreateInvoiceDto dto)
    {
        return new List<string>
        {
            $"Parking session duration is {dto.SessionDuration}",
            $"Total cost: {dto.Cost:0.00} EUR"
        };
    }

    private static List<string> GenerateInvoiceSummary(InvoiceModel invoice)
    {
        return new List<string>
        {
            $"Parking session duration is {invoice.SessionDuration}",
            $"Total cost: {invoice.Cost:0.00} EUR"
        };
    }

}