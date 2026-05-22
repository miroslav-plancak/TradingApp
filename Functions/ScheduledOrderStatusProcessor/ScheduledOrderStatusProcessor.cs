using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingApp.Domain;
using TradingApp.Domain.Models.Enums;

namespace ScheduledOrderStatusProcessor
{
    public class ScheduledOrderStatusProcessor
    {
        private readonly ILogger<ScheduledOrderStatusProcessor> _logger;
        private readonly TradingDbContext _tradingDbContext;

        public ScheduledOrderStatusProcessor(
            ILogger<ScheduledOrderStatusProcessor> logger,
            TradingDbContext tradingDbContext)
        {
            _logger = logger;
            _tradingDbContext = tradingDbContext;
        }

        [Function("ScheduledOrderStatusProcessor")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogWarning("ScheduledOrderStatusProcessor triggered at: {TriggerTime}",
                DateTimeOffset.UtcNow);

            var pendingAckOrders = await _tradingDbContext.Orders
                .Where(ao => ao.Status == OrderStatus.ACKNOWLEDGED)
                .ToListAsync();

            if (pendingAckOrders.Count == 0)
            {
                _logger.LogWarning("NoAcknowledgedOrders | No orders to promote to FILLED");
                return;
            }

            _logger.LogWarning("PromotingOrders | Found {Count} ACKNOWLEDGED orders to promote",
                pendingAckOrders.Count);

            foreach (var pendingAckOrder in pendingAckOrders)
            {
                _logger.LogWarning(
                    "PromotingOrder | CorrelationId: {CorrelationId} | OrderId: {OrderId} | ClientOrderId: {ClientOrderId} | ACKNOWLEDGED ? FILLED",
                    pendingAckOrder.CorrelationId, pendingAckOrder.Id, pendingAckOrder.ClientOrderId);

                pendingAckOrder.Status = OrderStatus.FILLED;
                pendingAckOrder.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _tradingDbContext.SaveChangesAsync();

            _logger.LogWarning("OrdersPromoted | Updated {Count} orders to FILLED",
                pendingAckOrders.Count);
        }
    }
}
