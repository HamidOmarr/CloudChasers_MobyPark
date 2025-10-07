using MobyPark;
using MobyPark.Models;
using MobyPark.Services.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMobyParkServices();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // TODO: When a frontend is attached, configure error handling here
    app.UseHsts();
}

// Enable Swagger to test API endpoints
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

// Attribute-based routing onlya
app.MapControllers();


var services = app.Services.GetRequiredService<ServiceStack>();

// var vehicle = new VehicleModel
// {
//     UserId = 1,
//     LicensePlate = "XYZ123",
//     Make = "Toyota",
//     Model = "Camry",
//     Color = "Blue",
//     Year = 2020,
//     CreatedAt = DateTime.UtcNow
// };
//
// await services.Vehicles.CreateVehicle(vehicle);

app.Run();
