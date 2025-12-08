using MobyPark.Models;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services.Interfaces;

namespace MobyPark.Services;

public class BusinessParkingRegistrationService : IBusinessParkingRegistrationService
{
    private readonly IRepository<BusinessParkingRegistrationModel> _registrationRepo;

    public BusinessParkingRegistrationService(IRepository<BusinessParkingRegistrationModel> registrationRepo)
    {
        _registrationRepo = registrationRepo;
    }

    public async Task<bool> CreateBusinessRegistrationForPlate()
    {
        return true;
    }

}