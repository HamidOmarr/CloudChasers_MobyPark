using MobyPark.Models.Access;

namespace MobyPark.Models.DataService;

public class DataService : IDataService
{
    public IParkingLotAccess ParkingLots { get; }
    public IPaymentAccess Payments { get; }
    public IReservationAccess Reservations { get; }
    public IUserAccess Users { get; }
    public IVehicleAccess Vehicles { get; }

    public DataService(IParkingLotAccess parkingLots, IPaymentAccess payments, IReservationAccess reservations, IUserAccess users, IVehicleAccess vehicles)
    {
        ParkingLots = parkingLots;
        Payments = payments;
        Reservations = reservations;
        Users = users;
        Vehicles = vehicles;
    }
}
