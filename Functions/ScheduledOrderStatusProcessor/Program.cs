using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradingApp.Domain;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add Key Vault support for local development
builder.Configuration.AddAzureKeyVault(
    new Uri("https://tradingapp-demo-kv.vault.azure.net/"),
    new DefaultAzureCredential());

//logging
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

//register DbContext
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlServer(builder.Configuration["SqlConnectionString"])
);

builder.Build().Run();