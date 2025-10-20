using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MobyPark.Models;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Models.Repositories.RepositoryStack;
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

        // Repository stack: Scoped to align with database connection lifecycle.
        services.AddScoped<IRepositoryStack, RepositoryStack>();


        // JWT Token Generator: Must be Singleton as it is stateless and reads configuration.
        services.AddSingleton<SessionService>();

        // Password Hasher: Must be Singleton as it is stateless and resource-intensive.
        services.AddSingleton<IPasswordHasher<UserModel>, PasswordHasher<UserModel>>();

        // Business Logic Services: Scoped to manage state per request.
        services.AddScoped<LicensePlateService>();
        services.AddScoped<ParkingLotService>();
        services.AddScoped<ParkingSessionService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<PermissionService>();
        services.AddScoped<ReservationService>();
        services.AddScoped<RolePermissionService>();
        services.AddScoped<RoleService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<UserPlateService>();
        services.AddScoped<UserService>();

        // ServiceStack: Scoped as it bundles Scoped services together.
        // services.AddScoped<ServiceStack>();
    }
}
