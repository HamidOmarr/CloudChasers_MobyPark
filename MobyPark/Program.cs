using MobyPark;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMobyParkServices();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // TODO: Replace with a proper global error handler later (using React)
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

app.Run();
