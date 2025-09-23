namespace MobyPark.Services.Services;

public class ServiceStack
{
    public ParkingSessionService ParkingSessions;
    public PaymentService Payments;
    public SessionService Sessions;
    public UserService Users;

    public ServiceStack(ParkingSessionService parkingSessions, PaymentService payments, SessionService sessions, UserService users)
    {
        ParkingSessions = parkingSessions;
        Payments = payments;
        Sessions = sessions;
        Users = users;
    }
}