using MobyPark.Models.Access;

namespace MobyPark.Models.DataService;

public class DataService : IDataService
{
    public IParkingLotAccess ParkingLots { get; }
    public IParkingSessionAccess ParkingSessions { get; }
    public IPaymentAccess Payments { get; }
    public IReservationAccess Reservations { get; }
    public IUserAccess Users { get; }
    public IVehicleAccess Vehicles { get; }

    public DataService(IParkingLotAccess parkingLots, IParkingSessionAccess parkingSessions, IPaymentAccess payments, IReservationAccess reservations, IUserAccess users, IVehicleAccess vehicles)
    {
        ParkingLots = parkingLots;
        ParkingSessions = parkingSessions;
        Payments = payments;
        Reservations = reservations;
        Users = users;
        Vehicles = vehicles;
    }
}
