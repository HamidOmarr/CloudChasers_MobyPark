using Microsoft.EntityFrameworkCore;

using MobyPark.Configuration;
using MobyPark.Models.DbContext;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppServices(builder);
builder.Services.AddDependencyInjection(builder.Configuration);
builder.Services.AddSwaggerAuthorization();

var app = builder.Build();

app.ConfigureMiddleware();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();