namespace MobyPark.Models.Repositories.Interfaces;

public interface ILicensePlateRepository : IRepository<LicensePlateModel>
{
    Task<LicensePlateModel?> GetByNumber(string licensePlateNumber);
}