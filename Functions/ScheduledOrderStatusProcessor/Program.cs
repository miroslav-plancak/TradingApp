using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradingApp.Domain;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

//logging
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

//register DbContext
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(Environment.GetEnvironmentVariable("SqlConnectionString"))
);

builder.Build().Run();
