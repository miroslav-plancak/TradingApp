using Microsoft.Extensions.DependencyInjection;
using TradingApp.Business.Interfaces.Repositories;
using TradingApp.Business.Interfaces.Services;
using TradingApp.Business.Services.Regular;
using TradingApp.Business.Services.Repository;

namespace TradingApp.Business
{
    public static class BusinessServiceCollectionExtensions
    {
        public static IServiceCollection RegisterBusiness(this IServiceCollection services) 
        {
            services.AddScoped<IOrderService, OrderService>()
                    .AddScoped<IOrderRepository, OrderRepository>();
            return services;
        }
    }
}
