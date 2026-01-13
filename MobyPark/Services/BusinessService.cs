using IbanNet;

using Microsoft.EntityFrameworkCore;

using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

namespace MobyPark.Services;

public class BusinessService : IBusinessService
{
    private readonly IRepository<BusinessModel> _businessRepo;
    private static readonly IbanValidator IbanValidator = new();

    public BusinessService(IRepository<BusinessModel> businessRepo)
    {
        _businessRepo = businessRepo;
    }

    //Create business
    public async Task<ServiceResult<ReadBusinessDto>> CreateBusinessAsync(CreateBusinessDto business)
    {
        try
        {
            var normalizedAddress = business.Address.Trim().ToLower();
            var addressTaken = await _businessRepo.Query().Where(x => x.Address.ToLower() == normalizedAddress).FirstOrDefaultAsync();
            if (addressTaken != null)
                return ServiceResult<ReadBusinessDto>.Conflict(
                    "There is already a business with that address in the system");
            var ibanValid = IbanValidator.Validate(business.IBAN.Trim());
            if (!ibanValid.IsValid) return ServiceResult<ReadBusinessDto>.BadRequest(
                "Invalid IBAN provided");

            var newBusiness = new BusinessModel
            {
                Name = business.Name.Trim(),
                Address = business.Address.Trim(),
                IBAN = business.IBAN.Trim()
            };

            _businessRepo.Add(newBusiness);
            await _businessRepo.SaveChangesAsync();

            return ServiceResult<ReadBusinessDto>.Ok(new ReadBusinessDto
            {
                Id = newBusiness.Id,
                Name = newBusiness.Name,
                Address = newBusiness.Address,
                IBAN = newBusiness.IBAN
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadBusinessDto>.Exception("Unexpected error occurred.");
        }
    }

    //Patch business
    public async Task<ServiceResult<ReadBusinessDto>> PatchBusinessAsync(long id, PatchBusinessDto businessPatch)
    {
        try
        {
            var business = await _businessRepo.FindByIdAsync(id);
            if (business is null) return ServiceResult<ReadBusinessDto>.NotFound("No business found with that id");
            if (!string.IsNullOrWhiteSpace(businessPatch.Address))
            {
                var normalizedAddress = businessPatch.Address.Trim().ToLower();
                var addressTaken = await _businessRepo.Query().Where(x => x.Address.ToLower() == normalizedAddress).FirstOrDefaultAsync();
                if (addressTaken is not null) return ServiceResult<ReadBusinessDto>.Conflict("New address already taken");
                business.Address = businessPatch.Address.Trim();
            }
            if (!string.IsNullOrWhiteSpace(businessPatch.Name)) business.Name = businessPatch.Name.Trim();
            if (!string.IsNullOrWhiteSpace(businessPatch.IBAN))
            {
                var ibanValid = IbanValidator.Validate(businessPatch.IBAN.Trim());
                if (!ibanValid.IsValid) return ServiceResult<ReadBusinessDto>.BadRequest(
                    "Invalid IBAN provided");
                business.IBAN = businessPatch.IBAN.Trim();
            }

            _businessRepo.Update(business);
            await _businessRepo.SaveChangesAsync();

            return ServiceResult<ReadBusinessDto>.Ok(new ReadBusinessDto
            {
                Id = business.Id,
                Name = business.Name,
                Address = business.Address,
                IBAN = business.IBAN
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadBusinessDto>.Exception("Unexpected error occurred.");
        }

    }

    //Delete business
    public async Task<ServiceResult<bool>> DeleteBusinessByIdAsync(long id)
    {
        try
        {
            var business = await _businessRepo.FindByIdAsync(id);
            if (business is null) return ServiceResult<bool>.NotFound("No business found with that id");
            _businessRepo.Deletee(business);
            await _businessRepo.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Exception("Unexpected error occurred.");
        }
    }

    //GetAllBusinesses
    public async Task<ServiceResult<List<ReadBusinessDto>>> GetAllAsync()
    {
        try
        {
            var list = await _businessRepo.ReadAllAsync();
            var dtoList = list.Select(x => new ReadBusinessDto
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                IBAN = x.IBAN
            }).ToList();

            return ServiceResult<List<ReadBusinessDto>>.Ok(dtoList);
        }
        catch (Exception)
        {
            return ServiceResult<List<ReadBusinessDto>>.Exception("Unexpected error occurred.");
        }
    }
    //GetBusinessById
    public async Task<ServiceResult<ReadBusinessDto>> GetBusinessByIdAsync(long id)
    {
        try
        {
            var business = await _businessRepo.FindByIdAsync(id);
            if (business is null) return ServiceResult<ReadBusinessDto>.NotFound("No business with that id found");
            return ServiceResult<ReadBusinessDto>.Ok(new ReadBusinessDto
            {
                Id = business.Id,
                Name = business.Name,
                Address = business.Address,
                IBAN = business.IBAN
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadBusinessDto>.Exception("Unexpected error occurred.");
        }
    }

    //GetBusinessByAddress
    public async Task<ServiceResult<ReadBusinessDto>> GetBusinessByAddressAsync(string address)
    {
        try
        {
            var normalizedAddress = address.Trim().ToLower();
            var business = await _businessRepo.Query().Where(x => x.Address.ToLower() == normalizedAddress).FirstOrDefaultAsync();
            if (business is null) return ServiceResult<ReadBusinessDto>.NotFound("No business with that address found");

            return ServiceResult<ReadBusinessDto>.Ok(new ReadBusinessDto
            {
                Id = business.Id,
                Name = business.Name,
                Address = business.Address,
                IBAN = business.IBAN
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadBusinessDto>.Exception("Unexpected error occurred.");
        }
    }
}