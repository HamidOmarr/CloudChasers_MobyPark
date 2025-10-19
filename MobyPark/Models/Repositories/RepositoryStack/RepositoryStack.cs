using MobyPark.Models.Repositories.Interfaces;

namespace MobyPark.Models.Repositories.RepositoryStack;

public class RepositoryStack : IRepositoryStack
{
    public ILicensePlateRepository LicensePlates { get; }
    public IParkingLotRepository ParkingLots { get; }
    public IParkingSessionRepository ParkingSessions { get; }
    public IPaymentRepository Payments { get; }
    public IPermissionRepository Permissions { get; }
    public IReservationRepository Reservations { get; }
    public IRolePermissionRepository RolePermissions { get; }
    public IRoleRepository Roles { get; }
    public ITransactionRepository Transactions { get; }
    public IUserPlateRepository UserPlates { get; }
    public IUserRepository Users { get; }

    public RepositoryStack(
        ILicensePlateRepository licensePlateRepository,
        IParkingLotRepository parkingLotRepository,
        IParkingSessionRepository parkingSessionRepository,
        IPaymentRepository paymentRepository,
        IPermissionRepository permissionRepository,
        IReservationRepository reservationRepository,
        IRolePermissionRepository rolePermissionRepository,
        IRoleRepository roleRepository,
        ITransactionRepository transactionRepository,
        IUserPlateRepository userPlateRepository,
        IUserRepository userRepository)
    {
        LicensePlates = licensePlateRepository;
        ParkingLots = parkingLotRepository;
        ParkingSessions = parkingSessionRepository;
        Payments = paymentRepository;
        Permissions = permissionRepository;
        Reservations = reservationRepository;
        RolePermissions = rolePermissionRepository;
        Roles = roleRepository;
        Transactions = transactionRepository;
        UserPlates = userPlateRepository;
        Users = userRepository;
    }
}