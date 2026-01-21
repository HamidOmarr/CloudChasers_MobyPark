using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using MobyPark.Models;
using MobyPark.Models.DbContext;
using MobyPark.Models.Repositories;
using MobyPark.Models.Repositories.Interfaces;
using MobyPark.Services;
using MobyPark.Services.Interfaces;

using Npgsql;

namespace MobyPark.Configuration;

public static class DiContainer
{
    public static void AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddRepositories();
        services.AddServices();
        services.AddUtilities();
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration
            .GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        dataSourceBuilder.MapEnum<ParkingLotStatus>()
            .MapEnum<ReservationStatus>()
            .MapEnum<ParkingSessionStatus>()
            .MapEnum<InvoiceStatus>("invoice_status");

        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(dataSource));
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services.AddScoped<ILicensePlateRepository, LicensePlateRepository>();
        services.AddScoped<IParkingSessionRepository, ParkingSessionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUserPlateRepository, UserPlateRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
    }

    private static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IParkingLotService, ParkingLotService>();
        services.AddScoped<IGateService, GateService>();
        services.AddScoped<IPreAuthService, PreAuthService>();
        services.AddScoped<IHotelPassService, HotelPassService>();
        services.AddScoped<IBusinessService, BusinessService>();
        services.AddScoped<IBusinessParkingRegistrationService, BusinessParkingRegistrationService>();
        services.AddScoped<ILicensePlateService, LicensePlateService>();
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
        services.AddScoped<IAutomatedInvoiceService, AutomatedInvoiceService>();
    }

    private static void AddUtilities(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher<UserModel>, PasswordHashingService>();
    }
}