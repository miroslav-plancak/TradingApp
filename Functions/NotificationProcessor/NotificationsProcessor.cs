using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Domain;
using TradingApp.Domain.Models.Entities.OrderNotificationSequences;
using TradingApp.Domain.Models.Entities.PendingFilledNotification;
using TradingApp.Events.Events;

namespace NotificationProcessor
{
    public class NotificationsProcessor
    {
        private readonly ILogger<NotificationsProcessor> _logger;
        private readonly TradingDbContext _tradingDbContext;

        private const string TopicName = "order_events_topic";
        private const string SubscriptionName = "notifications";

        public NotificationsProcessor(
            ILogger<NotificationsProcessor> logger,
            TradingDbContext tradingDbContext)
        {
            _logger = logger;
            _tradingDbContext = tradingDbContext;
        }

        [Function(nameof(NotificationsProcessor))]
        public async Task Run(
            [ServiceBusTrigger(
                TopicName,
                SubscriptionName,
                Connection = "ServiceBusConnection",
                IsSessionsEnabled = true)]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            var correlationId = message.CorrelationId ?? "UNKNOWN";

            _logger.LogWarning(
                "NotificationProcessor started | CorrelationId: {CorrelationId} | SessionId: {SessionId}",
                correlationId, message.SessionId);

            var orderEvent = JsonSerializer.Deserialize<OrderProcessedEvent>(
                message.Body.ToString());

            if (orderEvent == null)
            {
                _logger.LogWarning(
                    "OrderEventNull | CorrelationId: {CorrelationId}", correlationId);
                return;
            }

            _logger.LogWarning(
                "ReceivedEvent | CorrelationId: {CorrelationId} | Status: {Status} | Sequence: {Sequence}",
                correlationId, orderEvent.Status, orderEvent.Sequence);

            var tracking = await _tradingDbContext.OrderNotificationSequences
                .FirstOrDefaultAsync(x => x.ClientOrderId == orderEvent.ClientOrderId);

            var lastProcessedSequence = tracking?.LastProcessedSequence ?? 0;
          
            if (orderEvent.Sequence > lastProcessedSequence + 1)
            {
                _logger.LogWarning(
                    "OutOfOrder | Expected sequence {Expected} but got {Actual} | " +
                    "PersistingFilledToDB | CorrelationId: {CorrelationId}",
                    lastProcessedSequence + 1, orderEvent.Sequence, correlationId);

                var serializedOrderEventPayload = JsonSerializer.Serialize(orderEvent);

                _tradingDbContext.PendingFilledNotifications.Add(new PendingFilledNotification
                {
                    ClientOrderId = orderEvent.ClientOrderId,
                    EventPayload = serializedOrderEventPayload,
                    CorrelationId = correlationId,
                    StoredAt = DateTimeOffset.UtcNow
                });

                await _tradingDbContext.SaveChangesAsync();

                _logger.LogWarning(
                   "FilledPersistedToDB | ClientOrderId: {ClientOrderId} | CorrelationId: {CorrelationId}",
                   orderEvent.ClientOrderId, correlationId);

                return;
            }

            await ProcessNotification(orderEvent, correlationId);

            var pendingFilledOrder = await _tradingDbContext.PendingFilledNotifications
                .FirstOrDefaultAsync(x => x.ClientOrderId == orderEvent.ClientOrderId);

            if(pendingFilledOrder != null)
            {
                _logger.LogWarning(
                    "PendingFilledFound | Sending deferred FILLED | CorrelationId: {CorrelationId}",
                    correlationId);

                var deserializedpendingFilledOrder = 
                    JsonSerializer.Deserialize<OrderProcessedEvent>(pendingFilledOrder.EventPayload);

                if(deserializedpendingFilledOrder != null)
                {
                    await ProcessNotification(deserializedpendingFilledOrder, correlationId);
                }

                _tradingDbContext.PendingFilledNotifications.Remove(pendingFilledOrder);

                if (tracking != null)
                {
                    _tradingDbContext.OrderNotificationSequences.Remove(tracking);
                }

                await _tradingDbContext.SaveChangesAsync();

                return;
            }

            if (tracking == null && orderEvent.Status != "REJECTED")
            {
                _tradingDbContext.OrderNotificationSequences.Add(
                    new OrderNotificationSequence
                    {
                        ClientOrderId = orderEvent.ClientOrderId,
                        LastProcessedSequence = orderEvent.Sequence,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });

                _logger.LogWarning(
                    "TrackingCreated | Sequence: {Sequence} | CorrelationId: {CorrelationId}",
                    orderEvent.Sequence, correlationId);
            }
            else if (tracking != null)
            {
                tracking.LastProcessedSequence = orderEvent.Sequence;
                tracking.UpdatedAt = DateTimeOffset.UtcNow;

                if (orderEvent.Sequence == 2)
                {
                    _tradingDbContext.OrderNotificationSequences.Remove(tracking);

                    _logger.LogWarning(
                        "TrackingRemoved | FinalSequenceProcessed | CorrelationId: {CorrelationId}",
                        correlationId);
                }
            }

            await _tradingDbContext.SaveChangesAsync();
        }

        private async Task ProcessNotification(
            OrderProcessedEvent orderEvent,
            string correlationId)
        {
            _logger.LogWarning(
                "Sending notification for Order with CorrelationId: {CorrelationId} " +
                "| ClientOrderId {ClientOrderId} | OrderStatus: {Status}",
                correlationId, orderEvent.ClientOrderId, orderEvent.Status);

            await SendNotifications(orderEvent);

            _logger.LogWarning(
                "Notification sent for Order with CorrelationId: {CorrelationId} " +
                "| ClientOrderId {ClientOrderId} | OrderStatus: {Status}",
                correlationId, orderEvent.ClientOrderId, orderEvent.Status);
        }

        // ─────────────────────────────────────────────────────────────────
        // SendNotifications: the actual downstream notification logic.
        // Currently simulated with Task.Delay(1000).
        // In production: send email, Teams message, webhook, etc.
        // ─────────────────────────────────────────────────────────────────
        private async Task SendNotifications(OrderProcessedEvent orderEvent)
        {
            await Task.Delay(1000);
        }
    }
}