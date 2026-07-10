using Microsoft.Extensions.Logging.Configuration;
using ProxyAPI.Presentation.Extensions;
using ProxyAPI.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Logging.AddConfiguration();
builder.Configuration.AddJsonFile("ServiceDiscovery.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"ServiceDiscovery.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthenticationMiddleware();
app.MapControllers();

app.Run();
