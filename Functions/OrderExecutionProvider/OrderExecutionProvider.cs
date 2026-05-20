using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Domain;
using TradingApp.Domain.Models.Entities.UnpublishedTopicMessages;
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

        public OrderExecutionProvider
        (
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
        public async Task Run
        (
            [ServiceBusTrigger(
                queueName: "CREATE_ORDER_QUEUE",
                Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions
        )
        {
            var correlationId = message.CorrelationId ?? "CorrelationId";

            _logger.LogInformation(
                "OrderExecutionStarted | CorrelationId: {CorrelationId} | MessageId: {MessageId}",
                correlationId, message.MessageId);

            var payload = JsonSerializer.Deserialize<OrderPayload>(message.Body.ToString());

            if (payload == null)
            {
                _logger.LogError(
                    "InvalidPayload | CorrelationId: {CorrelationId} | MessageId: {MessageId}",
                    correlationId, message.MessageId);
                return;
            }

            var orderExists = await _tradingDbContext.Orders
                .AnyAsync(o => o.ClientOrderId == payload.ClientOrderId);

            if (!orderExists)
            {
                _logger.LogWarning(
                    "OrderNotFound | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId}",
                    correlationId, payload.ClientOrderId);
                return;
            }

            var random = new Random();
            var randomStatus = random.Next(2) == 0 ? OrderStatus.ACKNOWLEDGED : OrderStatus.REJECTED;

            var orderRowsProcessed = await _tradingDbContext.Orders
                .Where(x => x.ClientOrderId == payload.ClientOrderId && !x.IsProcessed)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(x => x.Status, randomStatus)
                    .SetProperty(x => x.IsProcessed, true)
                    .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow));

            if (orderRowsProcessed == 0)
            {
                _logger.LogInformation(
                    "OrderAlreadyProcessed | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId}",
                    correlationId, payload.ClientOrderId);
                return;
            }

            _logger.LogInformation(
                "OrderProcessed | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId} | Status: {Status}",
                correlationId, payload.ClientOrderId, randomStatus);

            await PublishOrderProcessedEvent(payload.ClientOrderId, randomStatus, correlationId);
        }

        private async Task PublishOrderProcessedEvent(Guid clientOrderId, OrderStatus randomStatus, string correlationId)
        {
            try
            {
                var sender = _serviceBusClient.CreateSender("order_events_topic");

                var eventPayload = new OrderProcessedEvent
                {
                    ClientOrderId = clientOrderId,
                    Status = randomStatus.ToString(),
                    ProcessedAt = DateTimeOffset.UtcNow
                };

                var messageBody = JsonSerializer.Serialize(eventPayload);

                var message = new ServiceBusMessage(messageBody)
                {
                    MessageId = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId, 
                    ContentType = "application/json",
                    Subject = "OrderProcessed"
                };

                await sender.SendMessageAsync(message);

                _logger.LogInformation(
                    "EventPublishedToTopic | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId} | Topic: order_events_topic",
                    correlationId, clientOrderId);
            }
            catch (ServiceBusException serviceBusException)
            {
                _logger.LogError(serviceBusException,
                    "TopicPublishFailed | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId} | Error: {Message}",
                    correlationId, clientOrderId, serviceBusException.Message);

                _tradingDbContext.UnpublishedTopicMessages.Add(new UnpublishedTopicMessage
                {
                    Id = Guid.NewGuid(),
                    ClientOrderId = clientOrderId,
                    OrderStatus = randomStatus,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CorrelationId = correlationId 
                });

                await _tradingDbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "SavedToUnpublishedTopicMessages | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId}",
                    correlationId, clientOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "EventPublishFailed | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId}",
                    correlationId, clientOrderId);
            }
        }
    }
}