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


// var services = app.Services.GetRequiredService<ServiceStack>();
// var transaction = new TransactionDataModel
// {
//     Amount = 15.00m,
//     Bank = "Test Bank",
//     Date = DateOnly.FromDateTime(DateTime.UtcNow),
//     Issuer = "Test Issuer",
//     Method = "Credit Card",
// };
//
// var payment = new PaymentModel
// {
//     Amount = 15.00m,
//     Completed = DateTime.UtcNow,
//     CoupledTo = "testuser",
//     CreatedAt = DateTime.UtcNow,
//     Hash = Guid.NewGuid().ToString("N"),
//     Initiator = "inituser",
//     TransactionData = transaction,
//     TransactionId = Guid.NewGuid().ToString("N")
// };
//
// await services.Payments.CreatePayment(payment);

app.Run();
