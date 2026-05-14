using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Events.Events;

namespace AuditLogProcessor
{
    public class AuditLogProcessor
    {
        private readonly ILogger<AuditLogProcessor> _logger;

        public AuditLogProcessor(ILogger<AuditLogProcessor> logger)
        {
            _logger = logger;
        }

        [Function(nameof(AuditLogProcessor))]
        public async Task Run(
            [ServiceBusTrigger(
            "order_events_topic", 
            "audit-log", 
            Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message
        )
        {
            _logger.LogInformation("AuditLogProcessor triggered.");

            var orderEvent = JsonSerializer.Deserialize<OrderProcessedEvent>(message.Body);

            if (orderEvent == null)
            {
                _logger.LogWarning("Received null orderEvent.");
                return;
            }

            _logger.LogInformation("Writing audit log for Order: {ClientOrderId}", orderEvent.ClientOrderId);

            await WriteAuditLog(orderEvent);

            _logger.LogInformation("Audit log written for Order: {ClientOrderId}", orderEvent.ClientOrderId);

        }

        private async Task WriteAuditLog(OrderProcessedEvent orderEvent)
        {
            await Task.Delay(1500);
            await Task.CompletedTask;
        }
    }
}