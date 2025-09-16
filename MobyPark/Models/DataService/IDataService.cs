using MobyPark.Models.Access;

namespace MobyPark.Models.DataService;

public interface IDataService
{
    public IParkingLotAccess ParkingLots { get; }
    public IPaymentAccess Payments { get; }
    public IReservationAccess Reservations { get; }
    public IUserAccess Users { get; }
    public IVehicleAccess Vehicles { get; }
}