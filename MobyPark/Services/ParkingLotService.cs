using MobyPark.DTOs.ParkingLot.Request;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

namespace MobyPark.Services;

public class ParkingLotService : IParkingLotService
{
    private readonly IRepository<ParkingLotModel> _parkingRepo;


    public ParkingLotService(IRepository<ParkingLotModel> parkingLots)
    {
        _parkingRepo = parkingLots;
    }

    public async Task<ServiceResult<ReadParkingLotDto>> GetParkingLotByAddressAsync(string address)
    {
        try
        {
            var normalized = address.Trim().ToLower();
            var lot = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == normalized)).FirstOrDefault();
            if (lot is null) return ServiceResult<ReadParkingLotDto>.NotFound($"No parking lot found at address {address}");

            return ServiceResult<ReadParkingLotDto>.Ok( new ReadParkingLotDto
            {
                Id = lot.Id,
                Name = lot.Name,
                Location = lot.Location,
                Address = lot.Address,
                Reserved = lot.Reserved,
                Capacity = lot.Capacity,
                Tariff = lot.Tariff,
                DayTariff = lot.DayTariff
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<ReadParkingLotDto>.Exception("Unexpected error occurred.");
        }

    }

    public async Task<ServiceResult<ReadParkingLotDto>> GetParkingLotByIdAsync(long id)
    {
        try
        {
            var lot = await _parkingRepo.FindByIdAsync(id);
            if (lot is null) return ServiceResult<ReadParkingLotDto>.NotFound($"No lot with id: {id} found.");

            return ServiceResult<ReadParkingLotDto>.Ok( new ReadParkingLotDto
            {
                Id = lot.Id,
                Name = lot.Name,
                Location = lot.Location,
                Address = lot.Address,
                Reserved = lot.Reserved,
                Capacity = lot.Capacity,
                Tariff = lot.Tariff,
                DayTariff = lot.DayTariff
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<ReadParkingLotDto>.Fail("Unexpected error occurred.");
        }

    }

    public async Task<ServiceResult<ReadParkingLotDto>> CreateParkingLotAsync(CreateParkingLotDto parkingLot)
    {
        var normalized = parkingLot.Address.Trim().ToLower();

        try
        {
            var exists = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == normalized)).FirstOrDefault();
            if (exists is not null) return ServiceResult<ReadParkingLotDto>.Fail("Address taken");

            var lot = new ParkingLotModel
            {
                Name = parkingLot.Name,
                Location = parkingLot.Location,
                Address = parkingLot.Address,
                Capacity = parkingLot.Capacity,
                Reserved = 0,
                Tariff = parkingLot.Tariff,
                DayTariff = parkingLot.DayTariff
            };

            _parkingRepo.Add(lot);
            await _parkingRepo.SaveChangesAsync();

            return ServiceResult<ReadParkingLotDto>.Ok(new ReadParkingLotDto{
                Id = lot.Id,
                Name = lot.Name,
                Location = lot.Location,
                Address = lot.Address,
                Reserved = lot.Reserved,
                Capacity = lot.Capacity,
                Tariff = lot.Tariff,
                DayTariff = lot.DayTariff
            })
            ;
        }
        catch (Exception ex)
        {
            return ServiceResult<ReadParkingLotDto>.Fail("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<ReadParkingLotDto>> PatchParkingLotByAddressAsync(string address, PatchParkingLotDto updateLot)
    {
        var normalized = address.Trim().ToLower();
        try
        {
            var exists = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == normalized)).FirstOrDefault();
            if (exists is null)
                return ServiceResult<ReadParkingLotDto>.NotFound("No parking lot was found with that address");

            if (updateLot.Address is not null)
            {
                var newAddressNormalized = updateLot.Address.Trim().ToLower();
                var addressTaken = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == newAddressNormalized && x.Id != exists.Id)).FirstOrDefault();
                if (addressTaken is not null) return ServiceResult<ReadParkingLotDto>.BadRequest("There is already a parking lot assigned to the new address.");
            }

            if (!string.IsNullOrWhiteSpace(updateLot.Name)) exists.Name = updateLot.Name;
            if (!string.IsNullOrWhiteSpace(updateLot.Location)) exists.Location = updateLot.Location;
            if (!string.IsNullOrWhiteSpace(updateLot.Address)) exists.Address = updateLot.Address;
            if (updateLot.Capacity.HasValue) exists.Capacity = updateLot.Capacity.Value;
            if (updateLot.Tariff.HasValue) exists.Tariff = updateLot.Tariff.Value;
            if (updateLot.DayTariff.HasValue) exists.DayTariff = updateLot.DayTariff.Value;

            _parkingRepo.Update(exists);
            await _parkingRepo.SaveChangesAsync();
            return ServiceResult<ReadParkingLotDto>.Ok(new ReadParkingLotDto{
                    Id = exists.Id,
                    Name = exists.Name,
                    Location = exists.Location,
                    Address = exists.Address,
                    Reserved = exists.Reserved,
                    Capacity = exists.Capacity,
                    Tariff = exists.Tariff,
                    DayTariff = exists.DayTariff
                });
        }

        catch (Exception ex)
        {
            return ServiceResult<ReadParkingLotDto>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<ReadParkingLotDto>> PatchParkingLotByIdAsync(long id, PatchParkingLotDto updateLot)
    {
        try
        {
            var exists = await _parkingRepo.FindByIdAsync(id);
            if (exists is null)
                return ServiceResult<ReadParkingLotDto>.NotFound("No parking lot was found with that id");

            if (updateLot.Address is not null)
            {
                var newAddressNormalized = updateLot.Address.Trim().ToLower();
                var addressTaken = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == newAddressNormalized && x.Id != exists.Id)).FirstOrDefault();
                if (addressTaken is not null) return ServiceResult<ReadParkingLotDto>.BadRequest("There is already a parking lot assigned to the new address.");
            }

            if (!string.IsNullOrWhiteSpace(updateLot.Name)) exists.Name = updateLot.Name;
            if (!string.IsNullOrWhiteSpace(updateLot.Location)) exists.Location = updateLot.Location;
            if (!string.IsNullOrWhiteSpace(updateLot.Address)) exists.Address = updateLot.Address;
            if (updateLot.Capacity.HasValue) exists.Capacity = updateLot.Capacity.Value;
            if (updateLot.Tariff.HasValue) exists.Tariff = updateLot.Tariff.Value;
            if (updateLot.DayTariff.HasValue) exists.DayTariff = updateLot.DayTariff.Value;

            _parkingRepo.Update(exists);
            await _parkingRepo.SaveChangesAsync();
            return ServiceResult<ReadParkingLotDto>.Ok(new ReadParkingLotDto{
                    Id = exists.Id,
                    Name = exists.Name,
                    Location = exists.Location,
                    Address = exists.Address,
                    Reserved = exists.Reserved,
                    Capacity = exists.Capacity,
                    Tariff = exists.Tariff,
                    DayTariff = exists.DayTariff
                });
        }

        catch (Exception ex)
        {
            return ServiceResult<ReadParkingLotDto>.Exception("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<bool>> DeleteParkingLotByIdAsync(long id)
    {
        try
        {
            var exists = await _parkingRepo.FindByIdAsync(id);
            if (exists is null) return ServiceResult<bool>.NotFound("No lot found with that id. Deletion failed.");

            _parkingRepo.Deletee(exists);
            await _parkingRepo.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<bool>> DeleteParkingLotByAddressAsync(string address)
    {
        try
        {
            var normalized = address.Trim().ToLower();
            var exists = (await _parkingRepo.GetByAsync(x => x.Address.ToLower() == normalized)).FirstOrDefault();
            if (exists is null) return ServiceResult<bool>.NotFound("No lot found with that address. Deletion failed.");

            _parkingRepo.Deletee(exists);
            await _parkingRepo.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail("Unexpected error occurred.");
        }
    }

    public async Task<ServiceResult<List<ReadParkingLotDto>>> GetAllParkingLotsAsync()
    {
        try
        {
            var lots = await _parkingRepo.ReadAllAsync();
            var lotList = lots.Select(p => new ReadParkingLotDto
            {
                Id = p.Id,
                Name = p.Name,
                Location = p.Location,
                Address = p.Address,
                Reserved = p.Reserved,
                Capacity = p.Capacity,
                Tariff = p.Tariff,
                DayTariff = p.DayTariff
            }).ToList();

            return ServiceResult<List<ReadParkingLotDto>>.Ok(lotList);

        }
        catch (Exception ex)
        {
            return ServiceResult<List<ReadParkingLotDto>>.Fail("Unexpected error occurred.");
        }
    }

}