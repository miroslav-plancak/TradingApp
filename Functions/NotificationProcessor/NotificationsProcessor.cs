using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Events.Events;

namespace NotificationProcessor
{
    public class NotificationsProcessor
    {
        private readonly ILogger<NotificationsProcessor> _logger;

        public NotificationsProcessor(ILogger<NotificationsProcessor> logger)
        {
            _logger = logger;
        }

        [Function(nameof(NotificationsProcessor))]
        public async Task Run
        (
            [ServiceBusTrigger(
            "order_events_topic",
          "notifications",
            Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message
        )
        {
            var correlationId = message.CorrelationId ?? "CorrelationId";

            _logger.LogWarning("NotificationProcessor started | CorrelationId: {CorrelationId}",
                        correlationId);

            var orderEvent = JsonSerializer.Deserialize<OrderProcessedEvent>(message.Body.ToString());

            if (orderEvent == null)
            {
                _logger.LogWarning("OrderEventNull | CorrelationId: {CorrelationId}", correlationId);
                return;
            }

            _logger.LogWarning("Sending notification for Order with CorrelationId: {CorrelationId} | ClientOrderId {ClientOrderId}",
                correlationId, orderEvent.ClientOrderId);
            
            await SendNotifications(orderEvent);

            _logger.LogWarning("Notification sent for Order with CorrelationId: {CorrelationId} | ClientOrderId {ClientOrderId}",
                correlationId, orderEvent.ClientOrderId);
        }

        private async Task SendNotifications(OrderProcessedEvent orderEvent)
        {
            await Task.Delay(1000);
            await Task.CompletedTask;
        }
    }
}
