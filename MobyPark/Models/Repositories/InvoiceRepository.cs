using Microsoft.EntityFrameworkCore;

using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class InvoiceRepository : Repository<InvoiceModel>, IInvoiceRepository
{
    public InvoiceRepository(AppDbContext context) : base(context) { }

    public async Task<InvoiceModel?> GetInvoiceModelByLicensePlate(string LicensePlateNumber) =>
        await DbSet.FirstOrDefaultAsync(invoice => invoice.LicensePlateId == LicensePlateNumber);


}