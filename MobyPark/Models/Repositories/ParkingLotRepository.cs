using Microsoft.EntityFrameworkCore;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories;

public class ParkingLotRepository : Repository<ParkingLotModel>, IParkingLotRepository
{
    public ParkingLotRepository(AppDbContext context) : base(context) { }

    public async Task<ParkingLotModel?> GetByName(string name) =>
        await DbSet.FirstOrDefaultAsync(parkingLot => parkingLot.Name == name);

    public async Task<ParkingLotModel?> GetParkingLotByID(int id) =>
        await DbSet.FirstOrDefaultAsync(parkingLot => parkingLot.Id == id);

    public async Task<ParkingLotModel?> GetParkingLotByAddress(string address) =>
        await DbSet.FirstOrDefaultAsync(parkingLot => parkingLot.Address == address);

    public async Task<int> AddParkingLotAsync(ParkingLotModel parkingLot)
    {
        await DbSet.AddAsync(parkingLot);
        await Context.SaveChangesAsync();
        return (int)parkingLot.Id;
    }

    public async Task<ParkingLotModel?> UpdateParkingLotByID(ParkingLotModel parkingLot, int id)
    {
        var existing = await DbSet.FirstOrDefaultAsync(lot => lot.Id == id);
        if (existing == null) return null;

        // Copy updated fields
        existing.Name = parkingLot.Name;
        existing.Location = parkingLot.Location;
        existing.Address = parkingLot.Address;
        existing.Capacity = parkingLot.Capacity;
        existing.Reserved = parkingLot.Reserved;
        existing.Tariff = parkingLot.Tariff;
        existing.DayTariff = parkingLot.DayTariff;

        await Context.SaveChangesAsync();
        return existing;
    }

    public async Task<ParkingLotModel?> UpdateParkingLotByAddress(ParkingLotModel parkingLot, string address)
    {
        var existing = await DbSet.FirstOrDefaultAsync(lot => lot.Address == address);
        if (existing == null) return null;

        existing.Name = parkingLot.Name;
        existing.Location = parkingLot.Location;
        existing.Address = parkingLot.Address;
        existing.Capacity = parkingLot.Capacity;
        existing.Reserved = parkingLot.Reserved;
        existing.Tariff = parkingLot.Tariff;
        existing.DayTariff = parkingLot.DayTariff;

        await Context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteParkingLotByID(int id)
    {
        var lot = await DbSet.FirstOrDefaultAsync(l => l.Id == id);
        if (lot == null) return false;

        DbSet.Remove(lot);
        await Context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteParkingLotByAddress(string address)
    {
        var lot = await DbSet.FirstOrDefaultAsync(l => l.Address == address);
        if (lot == null) return false;

        DbSet.Remove(lot);
        await Context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ParkingLotModel>> GetByLocation(string location) =>
        await DbSet.Where(parkingLot => parkingLot.Location.Contains(location)).ToListAsync();
}
