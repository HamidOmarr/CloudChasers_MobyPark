using Microsoft.AspNetCore.Identity;
using MobyPark.Models;
using MobyPark.Models.Access;
using MobyPark.Models.Access.DatabaseConnection;
using MobyPark.Models.DataService;
using MobyPark.Services;
using MobyPark.Services.Services;

namespace MobyPark.Configuration;

public static class DiContainer
{
    public static void AddMobyParkServices(this IServiceCollection services)
    {
        // Access: Scoped. New instance per HTTP request.
        services.AddScoped<IUserAccess, UserAccess>();
        services.AddScoped<IVehicleAccess, VehicleAccess>();
        services.AddScoped<IParkingLotAccess, ParkingLotAccess>();
        services.AddScoped<IReservationAccess, ReservationAccess>();
        services.AddScoped<IPaymentAccess, PaymentAccess>();
        services.AddScoped<IParkingSessionAccess, ParkingSessionAccess>();

        // Data Service: Scoped to align with database connection lifecycle.
        services.AddScoped<IDataAccess, DataAccess>();
        services.AddScoped<IDatabaseConnection, DatabaseConnection>();

        // JWT Token Generator: Must be Singleton as it is stateless and reads configuration.
        services.AddSingleton<SessionService>();

        // Password Hasher: Must be Singleton as it is stateless and resource-intensive.
        services.AddSingleton<IPasswordHasher<UserModel>, PasswordHasher<UserModel>>();

        // Business Logic Services: Scoped to manage state per request.
        services.AddScoped<ParkingSessionService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<UserService>();
        services.AddScoped<ParkingLotService>();
        services.AddScoped<ReservationService>();
        services.AddScoped<VehicleService>();

        // ServiceStack: Scoped as it bundles Scoped services together.
        services.AddScoped<ServiceStack>();
    }
}
