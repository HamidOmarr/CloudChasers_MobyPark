using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories.RepositoryStack;

public interface IRepositoryStack
{
    ILicensePlateRepository LicensePlates { get; }
    IParkingLotRepository ParkingLots { get; }
    IParkingSessionRepository ParkingSessions { get; }
    IPaymentRepository Payments { get; }
    IPermissionRepository Permissions { get; }
    IReservationRepository Reservations { get; }
    IRolePermissionRepository RolePermissions { get; }
    IRoleRepository Roles { get; }
    ITransactionRepository Transactions { get; }
    IUserPlateRepository UserPlates { get; }
    IUserRepository Users { get; }
}