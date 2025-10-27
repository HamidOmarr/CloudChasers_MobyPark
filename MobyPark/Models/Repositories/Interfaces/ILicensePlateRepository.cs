namespace MobyPark.Models.Repositories.Interfaces;

public interface ILicensePlateRepository : IRepository<LicensePlateModel>
{
    new Task<(bool success, string licensePlateNumber)> CreateWithId(LicensePlateModel entity);
    Task<LicensePlateModel?> GetByNumber(string licensePlateNumber);
}
