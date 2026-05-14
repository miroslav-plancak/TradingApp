using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Domain;
using TradingApp.Domain.Models.Enums;
using TradingApp.Events.Events;
using TradingApp.Events.Payloads;

namespace OrderExecutionProvider
{
    public class OrderExecutionProvider
    {
        private readonly ILogger<OrderExecutionProvider> _logger;
        private readonly TradingDbContext _tradingDbContext;
        private readonly ServiceBusClient _serviceBusClient;

        public OrderExecutionProvider(
            ILogger<OrderExecutionProvider> logger,
            TradingDbContext tradingDbContext,
            ServiceBusClient serviceBusClient
            )
        {
            _logger = logger;
            _tradingDbContext = tradingDbContext;
            _serviceBusClient = serviceBusClient;
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

            var orderExists = await _tradingDbContext.Orders
                 .AnyAsync(o => o.ClientOrderId == payload.ClientOrderId);

            if (!orderExists)
            {
                _logger.LogWarning("Order not found in local database: {ClientOrderId}. ", payload.ClientOrderId);
                return;
            }

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

            await PublishOrderProcessedEvent(payload.ClientOrderId, randomStatus);
        }

        private async Task PublishOrderProcessedEvent(Guid clientOrderId, OrderStatus randomStatus)
        {
            try
            {
                var sender = _serviceBusClient.CreateSender("ORDER_EVENTS_TOPIC");

                var eventPayload = new OrderProcessedEvent
                {
                    ClientOrderId = clientOrderId,
                    Status = randomStatus.ToString(),
                    ProcessedAt = DateTimeOffset.UtcNow
                };

                var messageBody = JsonSerializer.Serialize(eventPayload);
                var message = new ServiceBusMessage(messageBody)
                {
                    ContentType = "aplication/json",
                    Subject = "OrderProcessed"
                };

                await sender.SendMessageAsync(message);

                _logger.LogInformation(
                  "Published OrderProcessed event to topic for ClientOrderId: {ClientOrderId}",
                  clientOrderId);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex,
                  "Failed to publish OrderProcessed event for ClientOrderId: {ClientOrderId}",
                  clientOrderId);
            }
        }
    }
}
