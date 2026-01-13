using Microsoft.EntityFrameworkCore;

using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class LicensePlateRepository : Repository<LicensePlateModel>, ILicensePlateRepository
{
    public LicensePlateRepository(AppDbContext context) : base(context) { }

    public async Task<LicensePlateModel?> GetByNumber(string licensePlateNumber)
    {
        return await DbSet
            .FirstOrDefaultAsync(licensePlate => licensePlate.LicensePlateNumber == licensePlateNumber);
    }
}