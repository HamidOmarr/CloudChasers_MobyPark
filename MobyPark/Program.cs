using MobyPark;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMobyParkServices();
builder.Services.AddEndpointsApiExplorer();   // needed for Swagger
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // TODO: Replace with a proper global error handler later (using React)
    app.UseSwagger();                // exposes /swagger/v1/swagger.json
    app.UseSwaggerUI();              // exposes /swagger
    //app.UseHsts();
}


//app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

// Attribute-based routing only
app.MapControllers();

app.Run();
