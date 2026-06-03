using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        private readonly HttpClient _httpClient;
        private readonly string _teamsWebhookUrl;

        private const string TopicName = "order_events_topic";
        private const string SubscriptionName = "notifications";

        public NotificationsProcessor
        (
            ILogger<NotificationsProcessor> logger,
            TradingDbContext tradingDbContext,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory
        )
        {
            _logger = logger;
            _tradingDbContext = tradingDbContext;
            _httpClient = httpClientFactory.CreateClient();
            _teamsWebhookUrl = configuration["TeamsWebhookUrl"]!;
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

            await SendTeamsNotification(orderEvent);

            _logger.LogWarning(
                "Notification sent for Order with CorrelationId: {CorrelationId} " +
                "| ClientOrderId {ClientOrderId} | OrderStatus: {Status}",
                correlationId, orderEvent.ClientOrderId, orderEvent.Status);
        }

        // ─────────────────────────────────────────────────────────────────
        // SendTeamsNotification: the actual downstream notification logic.
        // Currently simulated with Task.Delay(1000).
        // In production: send email, Teams message, webhook, etc.
        // ─────────────────────────────────────────────────────────────────
        private async Task SendTeamsNotification(OrderProcessedEvent orderEvent)
        {
            var statusColor = orderEvent.Status switch
            {
                "ACKNOWLEDGED" => "0076D7", // blue
                "FILLED"       => "00B050", // green
                "REJECTED"     => "D70000", // red
                 _             => "808080"  // grey
            };

            var statusEmoji = orderEvent.Status switch
            {
                "ACKNOWLEDGED" => "🔵",
                "FILLED"       => "🟢",
                "REJECTED"     => "🔴",
                _              => "⚪"
            };

            var payload = new
            {
                type = "MessageCard",
                context = "http://schema.org/extensions",
                themeColor = statusColor,
                summary = $"Order: {orderEvent.ClientOrderId} status update: {orderEvent.Status}",

                sections = new[]
                {
                    new
                    {
                        activityTitle = $"{statusEmoji} Order Status Update",
                        activitySubTitle = $"Status changed to  **{orderEvent.Status}**",
                        facts = new[]
                        {
                            new { name = "Order ID:", value = orderEvent.ClientOrderId.ToString()},
                            new { name = "Status:", value = orderEvent.Status},
                            new { name = "Processed At:", value = orderEvent.ProcessedAt.ToString("yyyy-MM-dd HH:mm:ss UTC")}
                        },
                        markdown = true
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogWarning(
                "SendingTeamsNotification | ClientOrderId: {ClientOrderId} | Status: {Status}",
                orderEvent.ClientOrderId, orderEvent.Status);

            try 
            {
                var httpResponse = await _httpClient.PostAsync(_teamsWebhookUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                      "TeamsNotificationSent | ClientOrderId: {ClientOrderId} | Status: {Status}",
                      orderEvent.ClientOrderId, orderEvent.Status);
                }
                else
                {
                    var responseBody = await httpResponse.Content.ReadAsStringAsync();
                    _logger.LogError(
                      "TeamsNotificationFailed | ClientOrderId: {ClientOrderId} | Status: {Status} | Response: {Response}",
                      orderEvent.ClientOrderId, orderEvent.Status, responseBody);
                }

            }
            catch(Exception ex)
            {
                _logger.LogError(ex,
                   "TeamsNotificationException | ClientOrderId: {ClientOrderId}",
                   orderEvent.ClientOrderId);
            }
        }
    }
}