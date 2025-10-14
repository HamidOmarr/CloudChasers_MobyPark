using MobyPark.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

app.ConfigureRequestPipeline();

app.Run();