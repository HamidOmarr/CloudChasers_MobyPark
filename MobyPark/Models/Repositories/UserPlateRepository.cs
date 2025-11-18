using Microsoft.EntityFrameworkCore;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class UserPlateRepository : Repository<UserPlateModel>, IUserPlateRepository
{
    public UserPlateRepository(AppDbContext context) : base(context) { }

    public async Task<bool> AddPlateToUser(long userId, string plate)
    {
        var existingUserPlate = await DbSet
            .FirstOrDefaultAsync(userPlate =>
                userPlate.UserId == userId
                && userPlate.LicensePlateNumber == plate);
        if (existingUserPlate is not null) return false;

        bool isFirstPlate = userId != DeletedUserId && !await DbSet.Where(uPlate => uPlate.UserId == userId).AnyAsync();

        var userPlate = new UserPlateModel
        {
            UserId = userId,
            LicensePlateNumber = plate,
            IsPrimary = isFirstPlate,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await DbSet.AddAsync(userPlate);
        await Context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserPlateModel>> GetPlatesByUserId(long userId) =>
        await DbSet
            .Where(userPlate => userPlate.UserId == userId)
            .ToListAsync();

    public async Task<List<UserPlateModel>> GetPlatesByPlate(string plate) =>
        await DbSet
            .Where(userPlate => userPlate.LicensePlateNumber == plate)
            .ToListAsync();

    public async Task<UserPlateModel?> GetPrimaryPlateByUserId(long userId)
    {
        return await DbSet
            .FirstOrDefaultAsync(userPlate =>
                userPlate.UserId == userId
                && userPlate.IsPrimary
            );
    }

    public async Task<UserPlateModel?> GetByUserIdAndPlate(long userId, string plate) =>
        await DbSet
            .FirstOrDefaultAsync(userPlate =>
                userPlate.UserId == userId
                && userPlate.LicensePlateNumber == plate
            );
}
