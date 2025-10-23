using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MobyPark.Models;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;

namespace MobyPark.Configuration;

public static class DiContainer
{
    public static void AddMobyParkServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repository: Scoped. New instance per HTTP request.
        services.AddScoped<ILicensePlateRepository, LicensePlateRepository>();
        services.AddScoped<IParkingLotRepository, ParkingLotRepository>();
        services.AddScoped<IParkingSessionRepository, ParkingSessionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUserPlateRepository, UserPlateRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGateService, GateService>();
        services.AddScoped<IPreAuthService, PreAuthService>();

        // JWT Token Generator: Must be Singleton as it is stateless and reads configuration.
        services.AddSingleton<ISessionService, SessionService>();

        // Password Hasher: Must be Singleton as it is stateless and resource-intensive.
        services.AddSingleton<IPasswordHasher<UserModel>, PasswordHasher<UserModel>>();

        // Business Logic Services: Scoped to manage state per request.
        services.AddScoped<ILicensePlateService, LicensePlateService>();
        services.AddScoped<IParkingLotService, ParkingLotService>();
        services.AddScoped<IParkingSessionService, ParkingSessionService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IUserPlateService, UserPlateService>();
        services.AddScoped<IUserService, UserService>();
    }
}
