using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NotificationProcessor;

public class NotificationProcessor
{
    private readonly ILogger<NotificationProcessor> _logger;

    public NotificationProcessor(ILogger<NotificationProcessor> logger)
    {
        _logger = logger;
    }

    [Function(nameof(NotificationProcessor))]
    public async Task Run(
        [ServiceBusTrigger(
        "ORDER_EVENTS_TOPIC",
       "notification",
        Connection = "ServiceBusConection")]
        ServiceBusReceivedMessage message
    )
    {
        _logger.LogInformation("NotificationProcessor triggered.");
      
        await Task.Delay(300);
      
        var orderEvent = JsonSerializer.Deserialize<OrderProcessedEvent>(message.Body);

        if (orderEvent == null)
        {
            _logger.LogWarning("Received null orderEvent.");
            return;
        }

        _logger.LogInformation("Sending notification for Order: {ClientOrderId}", orderEvent.ClientOrderId);
        
        await SendNotifications(orderEvent);

        _logger.LogInformation("Notification sent for Order: {ClientOrderId}", orderEvent.ClientOrderId);
        
    }

    private async Task SendNotifications(OrderProcessedEvent orderEvent)
    {
        await Task.Delay(1000);
        await Task.CompletedTask;
    }

    public class OrderProcessedEvent
    {
        public Guid ClientOrderId { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
    }

}