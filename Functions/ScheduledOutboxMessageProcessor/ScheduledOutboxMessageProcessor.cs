using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Domain;
using TradingApp.Domain.Models.Entities.QuarantinedOutboxMessage;
using TradingApp.Domain.Models.Enums;

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
            _logger.LogInformation("ScheduledOutboxMessageProcessor triggered at: {triggerTime}.",
                DateTimeOffset.UtcNow);
           
            // PHASE 1: Quarantine messages that have exhausted 5 retries
            var exhaustedOutboxMessages = await _tradingDbContext.OutboxMessages
                .Where(x => x.ProcessedAt == null && x.RetryCount >= 5)
                .ToListAsync();

            foreach (var exOutboxMsg in exhaustedOutboxMessages)
            {
                Guid? clientOrderId = Guid.TryParse(exOutboxMsg.Payload, out var parsed) ? parsed : null;

                _tradingDbContext.QuarantinedOutboxMessages.Add(new QuarantinedOutboxMessage
                {
                    Id = Guid.NewGuid(),
                    OriginalOutboxMessageId = exOutboxMsg.Id,
                    ClientOrderId = clientOrderId,
                    Payload = exOutboxMsg.Payload,
                    Reason = exOutboxMsg.RetryReason,
                    FinalRetryCount = exOutboxMsg.RetryCount,
                    QuarantinedAt = DateTimeOffset.UtcNow,
                    ErrorMessage = exOutboxMsg.LastError

                });

                exOutboxMsg.ProcessedAt = DateTimeOffset.UtcNow;

                _logger.LogWarning(
                    "Quarantined outbox message {Id}, reason: {Reason}, retries: {Count}",
                    exOutboxMsg.Id, exOutboxMsg.RetryReason, exOutboxMsg.RetryCount);
            }
           
            // PHASE 2: Process pending messages
            var outboxMessages = await _tradingDbContext.OutboxMessages
                .Where(x => x.ProcessedAt == null && x.RetryCount < 5)
                .OrderBy(x => x.CreatedAt)
                .Take(50)
                .ToListAsync();

            bool isServiceBusHealthy = false;

            foreach (var outboxMessage in outboxMessages)
            {
                try
                {
                    if (Guid.TryParse(outboxMessage.Payload, out var clientOrderId))
                    {
                        var isProcessedAlready = await _tradingDbContext.Orders
                            .Where(x => x.ClientOrderId == clientOrderId)
                            .Select(x => x.IsProcessed)
                            .FirstOrDefaultAsync();

                        if (isProcessedAlready)
                        {
                            outboxMessage.ProcessedAt = DateTimeOffset.UtcNow;
                            continue;
                        }
                        else
                        {
                            await NotifyServiceBusCreateOrderQueue(clientOrderId);

                            //throw new Exception("ScheduledOutboxMessageProcessor is offline.");

                            outboxMessage.ProcessedAt = DateTimeOffset.UtcNow;
                            isServiceBusHealthy = true;
                        }
                    }
                    else
                    {
                        _logger.LogError("Invalid guid payload: {Payload}", outboxMessage.Payload);
                        outboxMessage.RetryCount++;
                        outboxMessage.RetryReason = OutboxRetryReason.InvalidPayload;
                    }
                }
                catch (ServiceBusException serviceBusException)
                {
                    _logger.LogError(serviceBusException, "Service Bus error for outbox message {Id}", outboxMessage.Id);
                    outboxMessage.RetryCount++;
                    outboxMessage.RetryReason = OutboxRetryReason.ServiceBusUnavailable;
                    outboxMessage.LastError = serviceBusException.Message;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox message {Id}", outboxMessage.Id);
                    outboxMessage.RetryCount++;
                    outboxMessage.RetryReason = OutboxRetryReason.Unknown;
                }
            }

            // PHASE 3: Auto-recovery — Service Bus is back online,
            //          resurrect transient-failure quarantined messages
            if (isServiceBusHealthy)
            {
                var resurrectCandidates = await _tradingDbContext.QuarantinedOutboxMessages
                    .Where(q => !q.IsResurrected
                             && !q.IsDiscarded
                             && q.Reason == OutboxRetryReason.ServiceBusUnavailable)
                    .ToListAsync();

                foreach (var candidate in resurrectCandidates)
                {
                    var originalOutboxMessage = await _tradingDbContext.OutboxMessages
                        .FirstOrDefaultAsync(x => x.Id == candidate.OriginalOutboxMessageId);

                    if (originalOutboxMessage != null)
                    {
                        originalOutboxMessage.ProcessedAt = null;     
                        originalOutboxMessage.RetryCount = 4;        
                        originalOutboxMessage.RetryReason = OutboxRetryReason.None;

                        candidate.IsResurrected = true;
                        candidate.ResurrectedAt = DateTimeOffset.UtcNow;
                        candidate.ResolutionNotes = "Auto-resurrected: Service Bus connectivity restored";

                        _logger.LogInformation(
                            "Resurrected outbox message {Id} from quarantine — Service Bus is healthy",
                            originalOutboxMessage.Id);
                    }
                }

                if (resurrectCandidates.Count > 0)
                {
                    _logger.LogInformation(
                        "Service Bus healthy — resurrected {Count} transient-failure messages",
                        resurrectCandidates.Count);
                }
            }

            //throw new Exception("ScheduledOutboxMessageProcessor's database is offline/unreachable.");
            await _tradingDbContext.SaveChangesAsync();
        }

        private async Task NotifyServiceBusCreateOrderQueue(Guid clientOrderId)
        {
            var payload = new { ClientOrderId = clientOrderId };
            var json = JsonSerializer.Serialize(payload);

            //throw new Exception("ServiceBusQueue is offline/unreachable.");

            await _sender.SendMessageAsync(new ServiceBusMessage(json));
        }
    }
}