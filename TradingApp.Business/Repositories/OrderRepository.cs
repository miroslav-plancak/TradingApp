using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingApp.Business.Extensions;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Repositories;
using TradingApp.Domain;
using TradingApp.Domain.Models.Entities;

namespace TradingApp.Business.Repositories
{
    public class OrderRepository : TradingAppBaseLoggerExtension<OrderRepository>, IOrderRepository
    {
        private readonly TradingDbContext _tradingDbContext;
        public OrderRepository(ITradingAppLogger logger, TradingDbContext tradingDbContext) : base(logger)
        {
            _tradingDbContext = tradingDbContext;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            LogEntryWithScope();

            order.Id = Guid.NewGuid();
            order.ClientOrderId = Guid.NewGuid();
            order.CreatedAt = DateTimeOffset.UtcNow;
            order.UpdatedAt = DateTimeOffset.UtcNow;

            _tradingDbContext.Orders.Add(order);
            await _tradingDbContext.SaveChangesAsync();

            LogExitWithScope();

            return order;
        }

        public async Task<Order> GetOrderByIdAsync(Guid orderId)
        {
            LogEntryWithScope();

            var result = await _tradingDbContext.Orders
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == orderId);

            LogExitWithScope();

            return result;
        }

        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            LogEntryWithScope();

            var result = await _tradingDbContext.Orders.ToListAsync();

            LogExitWithScope();
            return result;
        }
    }
}
