using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Domain;
using TradingApp.Events.Events;

namespace ScheduledUnpublishedTopicMessagesProcessor
{
    public class ScheduledUnpublishedTopicMessagesProcessor
    {
        private readonly ILogger<ScheduledUnpublishedTopicMessagesProcessor> _logger;
        private readonly TradingDbContext _tradingDbContext;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusSender _sender;

        public ScheduledUnpublishedTopicMessagesProcessor(
            ILogger<ScheduledUnpublishedTopicMessagesProcessor> logger,
            TradingDbContext tradingDbContext,
            IConfiguration configuration)
        {
            _logger = logger;
            _tradingDbContext = tradingDbContext;
            var connectionString = configuration["ServiceBusConnectionString"];
            _serviceBusClient = new ServiceBusClient(connectionString);
            _sender = _serviceBusClient.CreateSender("order_events_topic");
        }

        [Function("ScheduledUnpublishedTopicMessagesProcessor")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("ScheduledUnpublishedTopicMessagesProcessor triggered at: {TriggerTime}",
                DateTimeOffset.UtcNow);

            var unpublishedMessages = await _tradingDbContext.UnpublishedTopicMessages
                .Where(x => x.PublishedAt == null && x.RetryCount < 5)
                .OrderBy(x => x.CreatedAt)
                .Take(50)
                .ToListAsync();

            if (unpublishedMessages.Count == 0)
            {
                _logger.LogInformation("NoUnpublishedMessages | No messages to retry");
                return;
            }

            _logger.LogInformation("RetryingUnpublishedMessages | Found {Count} messages to retry",
                unpublishedMessages.Count);

            foreach (var unpublishedMessage in unpublishedMessages)
            {
                try
                {
                    _logger.LogInformation(
                        "RetryingTopicPublish | CorrelationId: {CorrelationId} | UnpublishedId: {UnpublishedId} | ClientOrderId: {ClientOrderId}",
                        unpublishedMessage.CorrelationId, unpublishedMessage.Id, unpublishedMessage.ClientOrderId);

                    var eventPayload = new OrderProcessedEvent
                    {
                        ClientOrderId = unpublishedMessage.ClientOrderId,
                        Status = unpublishedMessage.OrderStatus.ToString(),
                        ProcessedAt = unpublishedMessage.ProcessedAt
                    };

                    var messageBody = JsonSerializer.Serialize(eventPayload);

                    var message = new ServiceBusMessage(messageBody)
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        CorrelationId = unpublishedMessage.CorrelationId, // Pass tracker!
                        ContentType = "application/json",
                        Subject = "OrderProcessed"
                    };

                    await _sender.SendMessageAsync(message);

                    unpublishedMessage.PublishedAt = DateTimeOffset.UtcNow;

                    _logger.LogInformation(
                        "TopicPublishRetrySucceeded | CorrelationId: {CorrelationId} | UnpublishedId: {UnpublishedId}",
                        unpublishedMessage.CorrelationId, unpublishedMessage.Id);
                }
                catch (ServiceBusException sbEx)
                {
                    _logger.LogError(sbEx,
                        "TopicPublishRetryFailed | CorrelationId: {CorrelationId} | UnpublishedId: {UnpublishedId} | Error: {Message}",
                        unpublishedMessage.CorrelationId, unpublishedMessage.Id, sbEx.Message);

                    unpublishedMessage.RetryCount++;
                    unpublishedMessage.LastError = sbEx.Message;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "TopicPublishRetryFailed | CorrelationId: {CorrelationId} | UnpublishedId: {UnpublishedId}",
                        unpublishedMessage.CorrelationId, unpublishedMessage.Id);

                    unpublishedMessage.RetryCount++;
                }
            }

            await _tradingDbContext.SaveChangesAsync();

            _logger.LogInformation("RetryProcessingComplete | Processed {Count} unpublished messages",
                unpublishedMessages.Count);
        }
    }
}