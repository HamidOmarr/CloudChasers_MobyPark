using MobyPark.Models.Access;
using MobyPark.Models.DataService;
using MobyPark.Services;
using MobyPark.Services.DatabaseConnection;

namespace MobyPark;

public static class DiContainer
{
    public static void AddMobyParkServices(this IServiceCollection services)
    {
        // Access layer
        services.AddScoped<IUserAccess, UserAccess>();
        services.AddScoped<IVehicleAccess, VehicleAccess>();
        services.AddScoped<IParkingLotAccess, ParkingLotAccess>();
        services.AddScoped<IReservationAccess, ReservationAccess>();
        services.AddScoped<IPaymentAccess, PaymentAccess>();
        services.AddScoped<IParkingSessionAccess, ParkingSessionAccess>();

        // Business logic
        services.AddSingleton<SessionService>();
        services.AddScoped<ParkingSessionService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<UserService>();
        services.AddScoped<ParkingLotService>();
        services.AddScoped<ReservationService>();
        services.AddScoped<VehicleService>();
        services.AddScoped<PaymentPreauthService>();
        services.AddScoped<GateService>();
        services.AddScoped<Services.Services.ServiceStack>();

        // Data service
        services.AddScoped<IDataAccess, DataAccess>();

        // Database connection
        services.AddScoped<IDatabaseConnection, DatabaseConnection>();
    }
}