using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Domain;
using TradingApp.Domain.Models.Entities.Order;
using TradingApp.Domain.Models.Enums;

namespace OrderExecutionProvider
{
    public class OrderExecutionProvider
    {
        private readonly ILogger<OrderExecutionProvider> _logger;
        private readonly TradingDbContext _tradingDbContext;

        public OrderExecutionProvider(ILogger<OrderExecutionProvider> logger,TradingDbContext tradingDbContext)
        {
            _logger = logger;
            _tradingDbContext = tradingDbContext;
        }

        [Function("OrderExecutionProvider")]
        public async Task Run(
            [ServiceBusTrigger(
            queueName:"CREATE_ORDER_QUEUE",
            Connection = "ServiceBusConnection")]
            string messageBody
        )
        {
            _logger.LogInformation("OrderExecutionProvider started.");

            var payload = JsonSerializer.Deserialize<OrderPayload>(messageBody);

            if (payload == null) return;

            //var order = await _tradingDbContext.Orders
            //    .FirstOrDefaultAsync(x => x.ClientOrderId == payload.ClientOrderId);

            //if (order == null) return;

            //if (order.IsProcessed)
            //{
            //    _logger.LogInformation("Order already processed: {Id}", payload.ClientOrderId);
            //    return;
            //}

            //var random = new Random();
            //order.Status = random.Next(2) == 0 ? OrderStatus.ACKNOWLEDGED : OrderStatus.REJECTED;
            //order.UpdatedAt = DateTimeOffset.UtcNow;
            //order.IsProcessed = true;

            //await _tradingDbContext.SaveChangesAsync();

            var random = new Random();
            var randomStatus = random.Next(2) == 0 ? OrderStatus.ACKNOWLEDGED : OrderStatus.REJECTED;

            var orderRowsAffected = await _tradingDbContext.Orders
                .Where(x => x.ClientOrderId == payload.ClientOrderId && !x.IsProcessed)
                .ExecuteUpdateAsync(x => x
                .SetProperty(x => x.Status, randomStatus)
                .SetProperty(x => x.IsProcessed, true)
                .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow));

            if(orderRowsAffected == 0)
            {
                _logger.LogInformation("Order already processed by another instance: {Id}", payload.ClientOrderId);
                return;
            }

            _logger.LogInformation("Order processed successfully: {Id}", payload.ClientOrderId);
        }
    }
}
