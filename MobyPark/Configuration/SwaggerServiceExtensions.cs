using System.Reflection;

using Microsoft.OpenApi.Models;

namespace MobyPark.Configuration;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerAuthorization(this IServiceCollection services)
    {
        services.AddSwaggerGen(swaggerGenOptions =>
        {
            swaggerGenOptions.EnableAnnotations();

            swaggerGenOptions.SwaggerDoc("v1", new OpenApiInfo
            {
                // Title: "MobyPark API",
                // Version: "v1"
                // Description: "Parking management API for MobyPark."
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
                swaggerGenOptions.IncludeXmlComments(xmlPath);

            swaggerGenOptions.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token (e.g., 'Bearer eyJ...').\n" +
                "You can obtain a token by logging in or registering a new account via the endpoints.",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

            swaggerGenOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}