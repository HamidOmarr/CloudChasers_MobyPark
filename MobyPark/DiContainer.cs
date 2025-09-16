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

        // Data service
        services.AddScoped<IDataService, DataService>();

        // Database connection
        services.AddScoped<IDatabaseConnection, DatabaseConnection>();
    }
}