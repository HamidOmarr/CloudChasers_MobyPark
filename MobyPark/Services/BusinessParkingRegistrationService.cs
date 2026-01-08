using MobyPark.DTOs.Business;
using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;
using MobyPark.Services.Results;

namespace MobyPark.Services;

public class BusinessParkingRegistrationService : IBusinessParkingRegistrationService
{
    private readonly IRepository<BusinessParkingRegistrationModel> _regRepo; //registration repository
    private readonly IRepository<UserModel> _userRepo;
    private readonly IRepository<BusinessModel> _businessRepo;

    public BusinessParkingRegistrationService(IRepository<BusinessParkingRegistrationModel> registrationRepo, IRepository<UserModel> userRepo, IRepository<BusinessModel> businessRepo)
    {
        _regRepo = registrationRepo;
        _userRepo = userRepo;
        _businessRepo = businessRepo;
    }


    //AdminCreateBusinessRegistrationForPlateAsync
    public async Task<ServiceResult<ReadBusinessRegDto>> CreateBusinessRegistrationAdminAsync(CreateBusinessRegAdminDto bReg)
    {
        try
        {
            var businessExists = await _businessRepo.FindByIdAsync(bReg.BusinessId);
            if (businessExists is null)
                return ServiceResult<ReadBusinessRegDto>.NotFound("No business found with that id");
            var alreadyMade = (await _regRepo.GetByAsync(x =>
                    x.LicensePlateNumber == bReg.LicensePlateNumber && x.BusinessId == bReg.BusinessId))
                .FirstOrDefault();
            if (alreadyMade is not null)
                return ServiceResult<ReadBusinessRegDto>.Conflict(
                    "This licenseplate already has a business registration at this business, patch it instead");
            var alreadyActive = (await _regRepo.GetByAsync(x => x.LicensePlateNumber == bReg.LicensePlateNumber && x.Active == true)).FirstOrDefault();
            if (alreadyActive is not null) return ServiceResult<ReadBusinessRegDto>.Conflict(
                "This licenseplate already has an active business registration, deactivate that one first");
            var reg = new BusinessParkingRegistrationModel
            {
                BusinessId = bReg.BusinessId,
                LicensePlateNumber = bReg.LicensePlateNumber,
                Active = bReg.Active,
            };

            _regRepo.Add(reg);
            await _regRepo.SaveChangesAsync();
            return ServiceResult<ReadBusinessRegDto>.Ok(new ReadBusinessRegDto
            {
                Id = reg.Id,
                BusinessId = reg.BusinessId,
                LicensePlateNumber = reg.LicensePlateNumber,
                Active = reg.Active,
                LastSinceActive = reg.LastSinceActive
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadBusinessRegDto>.Exception("Unexpected error occurred.");
        }
    }

    //CreateBusinessRegistrationForPlateAsync
    public async Task<ServiceResult<ReadBusinessRegDto>> CreateBusinessRegistrationAsync(CreateBusinessRegDto bReg, long currentUserId)
    {
        try
        {
            var user = await _userRepo.FindByIdAsync(currentUserId);

            if (user is null) return ServiceResult<ReadBusinessRegDto>.NotFound("user not found");
            if (user.BusinessId is null)
            {
                return ServiceResult<ReadBusinessRegDto>.Conflict(
                    "This user is not authorized to create business parking registrations for any business");
            }
            var business = await _businessRepo.FindByIdAsync(user.BusinessId.Value);
            if (business is null) return ServiceResult<ReadBusinessRegDto>.NotFound("business not found");

            var alreadyActive = (await _regRepo.GetByAsync(x => x.LicensePlateNumber == bReg.LicensePlateNumber && x.Active == true)).FirstOrDefault();
            if (alreadyActive is not null) return ServiceResult<ReadBusinessRegDto>.Conflict(
                "This licenseplate already has an active business registration, deactivate that one first");

            var reg = new BusinessParkingRegistrationModel
            {
                BusinessId = business.Id,
                LicensePlateNumber = bReg.LicensePlateNumber,
                Active = bReg.Active
            };

            _regRepo.Add(reg);
            await _regRepo.SaveChangesAsync();

            return ServiceResult<ReadBusinessRegDto>.Ok(new ReadBusinessRegDto
            {
                Id = reg.Id,
                BusinessId = reg.BusinessId,
                LicensePlateNumber = reg.LicensePlateNumber,
                Active = reg.Active,
                LastSinceActive = reg.LastSinceActive
            });

        }
        catch (Exception)
        {
            return ServiceResult<ReadBusinessRegDto>.Exception("Unexpected error occurred.");
        }
    }

    //AdminPatchBusinessRegistrationAsync
    public async Task<ServiceResult<ReadBusinessRegDto>> SetBusinessRegistrationActiveAdminAsync(
        PatchBusinessRegDto bReg)
    {
        try
        {
            var reg = await _regRepo.FindByIdAsync(bReg.Id);
            if (reg is null)
                return ServiceResult<ReadBusinessRegDto>.NotFound("No business parking registration found with that id");
            reg.Active = bReg.Active;
            _regRepo.Update(reg);
            await _regRepo.SaveChangesAsync();

            return ServiceResult<ReadBusinessRegDto>.Ok(new ReadBusinessRegDto
            {
                Id = reg.Id,
                BusinessId = reg.BusinessId,
                LicensePlateNumber = reg.LicensePlateNumber,
                Active = reg.Active,
                LastSinceActive = reg.LastSinceActive
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadBusinessRegDto>.Exception("Unexpected error occurred.");
        }

    }

    //PatchBusinessRegistrationAsync
    public async Task<ServiceResult<ReadBusinessRegDto>> SetBusinessRegistrationActiveAsync(
        PatchBusinessRegDto bReg, long currentUserId)
    {
        try
        {
            var user = await _userRepo.FindByIdAsync(currentUserId);

            if (user is null) return ServiceResult<ReadBusinessRegDto>.NotFound("user not found");
            if (user.BusinessId is null)
            {
                return ServiceResult<ReadBusinessRegDto>.Conflict(
                    "This user is not authorized to update business parking registrations for any business");
            }
            var business = await _businessRepo.FindByIdAsync(user.BusinessId.Value);
            if (business is null) return ServiceResult<ReadBusinessRegDto>.NotFound("business not found");

            var reg = await _regRepo.FindByIdAsync(bReg.Id);
            if (reg is null)
                return ServiceResult<ReadBusinessRegDto>.NotFound("No business parking registration found with that id");
            if (reg.BusinessId != business.Id) return ServiceResult<ReadBusinessRegDto>.Conflict("This user is not authorized to update this registration");

            reg.Active = bReg.Active;
            _regRepo.Update(reg);
            await _regRepo.SaveChangesAsync();

            return ServiceResult<ReadBusinessRegDto>.Ok(new ReadBusinessRegDto
            {
                Id = reg.Id,
                BusinessId = reg.BusinessId,
                LicensePlateNumber = reg.LicensePlateNumber,
                Active = reg.Active,
                LastSinceActive = reg.LastSinceActive
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadBusinessRegDto>.Exception("Unexpected error occurred.");
        }

    }

    //AdminDeleteBusinessRegistrationAsync
    public async Task<ServiceResult<bool>> AdminDeleteBusinessRegistrationAsync(
        long id)
    {
        try
        {
            var reg = await _regRepo.FindByIdAsync(id);
            if (reg is null)
                return ServiceResult<bool>.NotFound("No business parking registration found with that id");
            _regRepo.Deletee(reg);
            await _regRepo.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Exception("Unexpected error occurred.");
        }

    }

    //GetBusinessRegistrationByIdAsync
    public async Task<ServiceResult<ReadBusinessRegDto>> GetBusinessRegistrationByIdAsync(long id)
    {
        try
        {
            var reg = await _regRepo.FindByIdAsync(id);
            if (reg is null) return ServiceResult<ReadBusinessRegDto>.NotFound("No registration found with that id");
            return ServiceResult<ReadBusinessRegDto>.Ok(new ReadBusinessRegDto
            {
                Id = reg.Id,
                BusinessId = reg.BusinessId,
                LicensePlateNumber = reg.LicensePlateNumber,
                Active = reg.Active,
                LastSinceActive = reg.LastSinceActive
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadBusinessRegDto>.Exception("Unexpected error occurred.");
        }
    }
    //GetBusinessRegistrationsByBusinessAsync
    public async Task<ServiceResult<List<ReadBusinessRegDto>>> GetBusinessRegistrationsByBusinessAsync(long id)
    {
        try
        {
            var reg = await _regRepo.GetByAsync(x => x.BusinessId == id);
            if (!reg.Any()) return ServiceResult<List<ReadBusinessRegDto>>.NotFound("No registrations found with that business id");

            var dtoList = reg.Select(x => new ReadBusinessRegDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                LicensePlateNumber = x.LicensePlateNumber,
                Active = x.Active,
                LastSinceActive = x.LastSinceActive
            }).ToList();

            return ServiceResult<List<ReadBusinessRegDto>>.Ok(dtoList);
        }
        catch (Exception)
        {
            return ServiceResult<List<ReadBusinessRegDto>>.Exception("Unexpected error occurred.");
        }
    }
    //GetBusinessRegistrationByLicensePlateAsync
    public async Task<ServiceResult<List<ReadBusinessRegDto>>> GetBusinessRegistrationByLicensePlateAsync(string licensePlate)
    {
        try
        {
            var reg = await _regRepo.GetByAsync(x => x.LicensePlateNumber == licensePlate);
            if (!reg.Any()) return ServiceResult<List<ReadBusinessRegDto>>.NotFound("No registrations found with that license plate number");

            var dtoList = reg.Select(x => new ReadBusinessRegDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                LicensePlateNumber = x.LicensePlateNumber,
                Active = x.Active,
                LastSinceActive = x.LastSinceActive
            }).ToList();

            return ServiceResult<List<ReadBusinessRegDto>>.Ok(dtoList);
        }
        catch (Exception)
        {
            return ServiceResult<List<ReadBusinessRegDto>>.Exception("Unexpected error occurred.");
        }
    }
    //GetActiveBusinessRegistrationByLicencePlateAsync
    public async Task<ServiceResult<ReadBusinessRegDto>> GetActiveBusinessRegistrationByLicencePlateAsync(string licensePlate)
    {
        try
        {
            var reg = (await _regRepo.GetByAsync(x => x.LicensePlateNumber == licensePlate && x.Active == true)
                ).FirstOrDefault();
            if (reg is null) return ServiceResult<ReadBusinessRegDto>.NotFound("No active business parking registration found for this licenseplate");
            return ServiceResult<ReadBusinessRegDto>.Ok(new ReadBusinessRegDto
            {
                Id = reg.Id,
                BusinessId = reg.BusinessId,
                LicensePlateNumber = reg.LicensePlateNumber,
                Active = reg.Active,
                LastSinceActive = reg.LastSinceActive
            });
        }
        catch (Exception)
        {
            return ServiceResult<ReadBusinessRegDto>.Exception("Unexpected error occurred.");
        }
    }

}