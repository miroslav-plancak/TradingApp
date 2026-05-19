using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Domain;
using TradingApp.Events.Events;

namespace ScheduledUnpublishedTopicMessagesProcessor;

public class ScheduledUnpublishedTopicMessagesProcessor
{
    private readonly ILogger _logger;
    private readonly TradingDbContext _tradingDbContext;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ScheduledUnpublishedTopicMessagesProcessor
    (
        ILoggerFactory loggerFactory, 
        TradingDbContext tradingDbContext,
        IConfiguration configuration
    )
    {
        _logger = loggerFactory.CreateLogger<ScheduledUnpublishedTopicMessagesProcessor>();
        _tradingDbContext = tradingDbContext;
        var connectionString = configuration["ServiceBusConnectionString"];
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender("order_events_topic");
    }

    [Function("ScheduledUnpublishedTopicMessagesProcessor")]
    public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("ScheduledUnpublishedTopicMessagesProcessor triggered at: {triggerTime}.",
                DateTimeOffset.UtcNow);

        var pendingMesages = await _tradingDbContext.UnpublishedTopicMessages
            .Where(x => x.PublishedAt == null && x.RetryCount < 5)
            .OrderBy(x => x.CreatedAt)
            .Take(50)
            .ToListAsync();

        if (pendingMesages.Count == 0) return;

        foreach(var pendingMsg in pendingMesages)
        {
            try
            {
                var eventPayload = new OrderProcessedEvent
                {
                    ClientOrderId = pendingMsg.ClientOrderId,
                    Status = pendingMsg.OrderStatus.ToString(),
                    ProcessedAt = DateTimeOffset.UtcNow
                };

                var messageBody = JsonSerializer.Serialize(eventPayload);
                var message = new ServiceBusMessage(messageBody)
                {
                    ContentType = "application/json",
                    Subject = "OrderProcessed"
                };

                await _sender.SendMessageAsync(message);

                pendingMsg.PublishedAt = DateTimeOffset.UtcNow;

                _logger.LogInformation("ScheduledUnpublishedTopicMessagesProcessor message publised at: {currentTime}.",
                 DateTimeOffset.UtcNow);
            }
            catch(ServiceBusException serviceBusException) 
            {
                pendingMsg.RetryCount++;
                pendingMsg.LastError = serviceBusException.Message;
             
                _logger.LogError(serviceBusException, "Retry publish failed for: {clientOrderId}, attempt{retryCount}",
                    pendingMsg.ClientOrderId,
                    pendingMsg.RetryCount);
            }
            catch (Exception ex) 
            {
                pendingMsg.RetryCount++;
                pendingMsg.LastError = ex.Message;

                _logger.LogError(ex,
                    "Unexpected error on retry for: {clientOrderId}",
                  pendingMsg.ClientOrderId);
            }
        }

        await _tradingDbContext.SaveChangesAsync();
    }
}