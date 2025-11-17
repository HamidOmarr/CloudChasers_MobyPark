using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MobyPark.Configuration;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecretKey = configuration["Jwt:Key"]
                           ?? throw new InvalidOperationException("JWT Secret Key 'Jwt:Key' is missing in configuration. It must be set via secrets.");
        var issuer = configuration["Jwt:Issuer"] ?? "MobyParkAPI";
        var audience = configuration["Jwt:Audience"] ?? "MobyParkUsers";

        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(entry => entry.Errors)
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ?
                            error.Exception?.Message ?? "A validation error occurred." :
                            error.ErrorMessage)
                        .ToList();

                    return new BadRequestObjectResult(new { errors });
                };
            });

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanManageConfig",
                policy => { policy.RequireClaim("Permission", "CONFIG:MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanManageUsers",
                policy => { policy.RequireClaim("Permission", "USERS:MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanReadUsers",
                policy => { policy.RequireClaim("Permission", "USERS:READ"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanUserSelfManage",
                policy => { policy.RequireClaim("Permission", "USERS:SELF_MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanManageParkingLots",
                policy => { policy.RequireClaim("Permission", "LOTS:MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanReadParkingLots",
                policy => { policy.RequireClaim("Permission", "LOTS:READ"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanViewAllFinance",
                policy => { policy.RequireClaim("Permission", "FINANCE:VIEW_ALL"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanManageParkingSessions",
                policy => { policy.RequireClaim("Permission", "SESSIONS:MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanReadAllParkingSessions",
                policy => { policy.RequireClaim("Permission", "SESSIONS:READ_ALL"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanManageReservations",
                policy => { policy.RequireClaim("Permission", "RESERVATIONS:MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanSelfManageReservations",
                policy => { policy.RequireClaim("Permission", "RESERVATIONS:SELF_MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanManagePlates",
                policy => { policy.RequireClaim("Permission", "PLATES:MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanSelfManagePlates",
                policy => { policy.RequireClaim("Permission", "PLATES:SELF_MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanProcessPayments",
                policy => { policy.RequireClaim("Permission", "PAYMENTS:PROCESS"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanCancelReservations",
                policy => { policy.RequireClaim("Permission", "RESERVATIONS:CANCEL"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanViewSelfFinance",
                policy => { policy.RequireClaim("Permission", "FINANCE:VIEW_SELF"); });
        
        services.AddMobyParkServices(configuration);
        services.AddSwaggerAuthorization();

        return services;
    }
}