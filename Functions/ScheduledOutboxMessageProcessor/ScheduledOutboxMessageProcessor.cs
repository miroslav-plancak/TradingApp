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

            await QuarantineExhaustedMessages();

            bool isServiceBusHealthy = await ProcessPendingMessages();

            if (isServiceBusHealthy)
            {
                await AutoRecoverResurrectedMessages();
            }
           
            await _tradingDbContext.SaveChangesAsync();
        }

        private async Task QuarantineExhaustedMessages()
        {
            var exhaustedOutboxMessages = await _tradingDbContext.OutboxMessages
                .Where(x => x.ProcessedAt == null && x.RetryCount >= 5)
                .ToListAsync();

            if (exhaustedOutboxMessages.Count == 0) return;

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
        }

        private async Task<bool> ProcessPendingMessages()
        {
            var outboxMessages = await _tradingDbContext.OutboxMessages
                .Where(x => x.ProcessedAt == null && x.RetryCount < 5)
                .OrderBy(x => x.CreatedAt)
                .Take(50)
                .ToListAsync();

            if (outboxMessages.Count == 0) return false;

            var clientOrderIds = outboxMessages
                .Where(x => Guid.TryParse(x.Payload, out _))
                .Select(x => Guid.Parse(x.Payload))
                .ToHashSet();

            var alreadyProcessedOrders = new HashSet<Guid>();

            if (clientOrderIds.Count > 0)
            {
                var processedOrders = await _tradingDbContext.Orders
                    .Where(x => clientOrderIds.Contains(x.ClientOrderId) && x.IsProcessed)
                    .Select(x => x.ClientOrderId)
                    .ToListAsync();

                alreadyProcessedOrders = new HashSet<Guid>(processedOrders);
            }

            bool isServiceBusHealthy = false;

            foreach (var outboxMessage in outboxMessages)
            {
                try
                {
                    if (Guid.TryParse(outboxMessage.Payload, out var clientOrderId))
                    {
                        if (alreadyProcessedOrders.Contains(clientOrderId))
                        {
                            outboxMessage.ProcessedAt = DateTimeOffset.UtcNow;
                            continue;
                        }

                        await NotifyServiceBusCreateOrderQueue(clientOrderId);
                        outboxMessage.ProcessedAt = DateTimeOffset.UtcNow;
                        isServiceBusHealthy = true;
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

            return isServiceBusHealthy;
        }

        private async Task AutoRecoverResurrectedMessages()
        {
            var resurrectCandidates = await _tradingDbContext.QuarantinedOutboxMessages
                .Where(q => !q.IsResurrected
                         && !q.IsDiscarded
                         && q.Reason == OutboxRetryReason.ServiceBusUnavailable)
                .ToListAsync();

            if (resurrectCandidates.Count == 0) return;

            var originalMessageIds = resurrectCandidates
                .Select(c => c.OriginalOutboxMessageId)
                .ToHashSet();

            var originalMessages = await _tradingDbContext.OutboxMessages
                .Where(x => originalMessageIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            foreach (var candidate in resurrectCandidates)
            {
                if (originalMessages.TryGetValue(candidate.OriginalOutboxMessageId, out var originalOutboxMessage))
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

            _logger.LogInformation(
                "Service Bus healthy — resurrected {Count} transient-failure messages",
                resurrectCandidates.Count);
        }

        private async Task NotifyServiceBusCreateOrderQueue(Guid clientOrderId)
        {
            var payload = new { ClientOrderId = clientOrderId };
            var json = JsonSerializer.Serialize(payload);
            await _sender.SendMessageAsync(new ServiceBusMessage(json));
        }
    }
}