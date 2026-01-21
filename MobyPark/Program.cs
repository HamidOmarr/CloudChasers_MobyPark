using MobyPark.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppServices(builder);
builder.Services.AddDependencyInjection(builder.Configuration);
builder.Services.AddSwaggerAuthorization();

var app = builder.Build();

app.ConfigureMiddleware();

app.Run();