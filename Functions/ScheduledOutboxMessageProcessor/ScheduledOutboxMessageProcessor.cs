using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Domain;

namespace ScheduledOutboxMessageProcessor
{
    public class ScheduledOutboxMessageProcessor
    {
        private readonly ILogger _logger;
        private readonly TradingDbContext _tradingDbContext;
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;

        public ScheduledOutboxMessageProcessor
            (
            ILoggerFactory loggerFactory, 
            TradingDbContext tradingDbContext,
            IConfiguration configuration
            )
        {
            _logger = loggerFactory.CreateLogger<ScheduledOutboxMessageProcessor>();
            _tradingDbContext = tradingDbContext;
            var connectionString = configuration["ServiceBusConnectionString"];
            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender("CREATE_ORDER_QUEUE");
        }

        [Function("ScheduledOutboxMessageProcessor")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("ScheduledOutboxMessageProcessor tiggered at: {triggerTime}.", DateTimeOffset.UtcNow);

            var messages = await _tradingDbContext.OutboxMessages
                .Where(x => x.ProcessedAt == null && x.RetryCount < 5)
                .OrderBy(x => x.CreatedAt)
                .Take(50)
                .ToListAsync();

            foreach(var message in messages)
            {
                try
                {
                    if(Guid.TryParse(message.Payload, out var clientOrderId))
                    {
                        var isProcessedAlready = await _tradingDbContext.Orders
                            .Where(x => x.ClientOrderId == clientOrderId)
                            .Select(x => x.IsProcessed)
                            .FirstOrDefaultAsync();

                        if (isProcessedAlready)
                        {
                            message.ProcessedAt = DateTimeOffset.UtcNow;
                            continue;
                        }
                        else 
                        {
                            await NotifyServiceBusCreateOrderQueue(clientOrderId);
                            message.ProcessedAt = DateTimeOffset.UtcNow;
                        }

                    }
                    else
                    {
                        _logger.LogError("Invalid guid payload: {Payload} ", message.Payload);
                        message.RetryCount++;
                    }

                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "Failed to Process outbox message {Id}", message.Id);
                    message.RetryCount++;
                }
            }

            await _tradingDbContext.SaveChangesAsync();
        }

        private async Task NotifyServiceBusCreateOrderQueue(Guid clientOrderId)
        {
            var payload = new { ClientOrderId = clientOrderId };
            var json = JsonSerializer.Serialize(payload);

            await _sender.SendMessageAsync(new ServiceBusMessage(json));
        }
    }
}