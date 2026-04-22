using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradingApp.Domain;

var builder = FunctionsApplication.CreateBuilder(args);

// Configure logging/telemetry
builder.ConfigureFunctionsWebApplication();
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Register DbContext
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(Environment.GetEnvironmentVariable("SqlConnectionString")));

builder.Build().Run();
