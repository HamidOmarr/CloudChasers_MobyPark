using Microsoft.EntityFrameworkCore;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class LicensePlateRepository : Repository<LicensePlateModel>, ILicensePlateRepository
{
    public LicensePlateRepository(AppDbContext context) : base(context) { }

    public async Task<(bool success, string licensePlateNumber)> CreateWithId(LicensePlateModel entity)
    {
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();

        return (true, entity.LicensePlateNumber);
    }

    public async Task<LicensePlateModel?> GetByNumber(string licensePlateNumber)
    {
        return await DbSet
            .FirstOrDefaultAsync(licensePlate => licensePlate.LicensePlateNumber == licensePlateNumber);
    }
}