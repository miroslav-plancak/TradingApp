using Microsoft.Extensions.DependencyInjection;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Repositories;
using TradingApp.Business.Interfaces.Services;
using TradingApp.Business.Logger;
using TradingApp.Business.Repositories;
using TradingApp.Business.Services.Regular;

namespace DeadLetterQueueProcessor
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterDeadLetterService(this IServiceCollection services) 
        {
            services.AddScoped<ITradingAppLogger, TradingAppLogger>();
            services.AddScoped<IDeadLetterService, DeadLetterService>();
            services.AddScoped<IDeadLetterRepository, DeadLetterRepository>();
            return services;
        }
    }
}
