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
            .AddPolicy("CanReadUsers",
                policy => { policy.RequireClaim("Permission", "USERS:READ"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanReadParkingLots",
                policy => { policy.RequireClaim("Permission", "LOTS:READ"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanManageParkingLots",
                policy => { policy.RequireClaim("Permission", "LOTS:MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanManageParkingSessions",
                policy => { policy.RequireClaim("Permission", "SESSIONS:MANAGE"); });

        services.AddAuthorizationBuilder()
            .AddPolicy("CanProcessPayments",
                policy => { policy.RequireClaim("Permission", "PAYMENTS:PROCESS"); });


        services.AddMobyParkServices(configuration);
        services.AddSwaggerAuthorization();

        return services;
    }
}