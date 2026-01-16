using System.Formats.Asn1;

namespace MobyPark.Models.Repositories.Interfaces;

public interface IInvoiceRepository : IRepository<InvoiceModel>
{
    Task<InvoiceModel?> GetInvoiceModelByLicensePlate(string LicensePlateNumber);
}