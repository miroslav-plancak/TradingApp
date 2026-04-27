using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingApp.Domain;
using TradingApp.Domain.Models.Enums;

namespace ScheduledOrderStatusProcessor 
{ 

    public class ScheduledOrderStatusProcessor
    {
        private readonly ILogger _logger;
        private readonly TradingDbContext _tradingDbContext;

        public ScheduledOrderStatusProcessor(ILoggerFactory loggerFactory, TradingDbContext tradingDbContext)
        {
            _logger = loggerFactory.CreateLogger<ScheduledOrderStatusProcessor>();
            _tradingDbContext = tradingDbContext;
        }

        [Function("ScheduledOrderStatusProcessor")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("ScheduledOrderStatusProcessor tiggered at: {triggerTime}.", DateTime.UtcNow);

            var pendingAckOrders = await _tradingDbContext.Orders
                .Where(ao => ao.Status == OrderStatus.ACKNOWLEDGED)
                .ToListAsync();

            if (pendingAckOrders.Count == 0)
            {
                _logger.LogInformation("No ACKNOWLEDGED status orders found.");
                return;
            }

            foreach (var pendingAckOrder in pendingAckOrders)
            {
                pendingAckOrder.Status = OrderStatus.FILLED;
                pendingAckOrder.UpdatedAt = DateTime.UtcNow;
            }

            await _tradingDbContext.SaveChangesAsync();

            _logger.LogInformation("Updated {count} ACKNOWLEDGED orders to FILLED.", pendingAckOrders.Count);
        }
    }
}