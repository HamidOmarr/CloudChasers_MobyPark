namespace MobyPark.Services.Services;

public class ServiceStack
{
    public ParkingLotService ParkingLots;
    public ParkingSessionService ParkingSessions;
    public PaymentService Payments;
    public ReservationService Reservations;
    public SessionService Sessions;
    public UserService Users;
    public VehicleService Vehicles;

    public ServiceStack(ParkingLotService parkingLots, ParkingSessionService parkingSessions, PaymentService payments, ReservationService reservations, SessionService sessions, UserService users, VehicleService vehicles)
    {
        ParkingLots = parkingLots;
        ParkingSessions = parkingSessions;
        Payments = payments;
        Reservations = reservations;
        Sessions = sessions;
        Users = users;
        Vehicles = vehicles;
    }
}