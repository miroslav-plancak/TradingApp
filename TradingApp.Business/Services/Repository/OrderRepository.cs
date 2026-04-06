using System;
using System.Threading.Tasks;
using TradingApp.Business.Extensions;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Repositories;
using TradingApp.Domain;
using TradingApp.Domain.Models.Entities;

namespace TradingApp.Business.Services.Repository
{
    public class OrderRepository : TradingAppBaseLoggerExtension<OrderRepository>, IOrderRepository
    {
        private readonly TradingDbContext _tradingDbContext;
        public OrderRepository(ITradingAppLogger logger, TradingDbContext tradingDbContext) : base(logger)
        {
            _tradingDbContext = tradingDbContext;
        }

        public async Task CreateOrderAsync(Order order)
        {
            LogEntryWithScope();

            order.Id = Guid.NewGuid();
            order.ClientOrderId = Guid.NewGuid();
            order.CreatedAt = DateTimeOffset.UtcNow;
            order.UpdatedAt = DateTimeOffset.UtcNow;

            _tradingDbContext.Orders.Add(order);

            LogExitWithScope();

            await _tradingDbContext.SaveChangesAsync();
        }

        public Task<Order> GetOrderByIdAsync(Order order)
        {
            throw new System.NotImplementedException();
        }

        public Task<Order> GetOrdersAsync(Order order)
        {
            throw new System.NotImplementedException();
        }
    }
}
