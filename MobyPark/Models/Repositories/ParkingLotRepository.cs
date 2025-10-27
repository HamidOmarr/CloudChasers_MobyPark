using Microsoft.EntityFrameworkCore;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class ParkingLotRepository : Repository<ParkingLotModel>, IParkingLotRepository
{
    public ParkingLotRepository(AppDbContext context) : base(context) { }

    public async Task<ParkingLotModel?> GetByName(string name) =>
        await DbSet.FirstOrDefaultAsync(parkingLot => parkingLot.Name == name);

    public async Task<List<ParkingLotModel>> GetByLocation(string location) =>
        await DbSet.Where(parkingLot => parkingLot.Location.Contains(location)).ToListAsync();
}
