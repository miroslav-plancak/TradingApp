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
        private readonly ILogger<ScheduledOutboxMessageProcessor> _logger;
        private readonly TradingDbContext _tradingDbContext;
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;

        public ScheduledOutboxMessageProcessor
        (
            ILogger<ScheduledOutboxMessageProcessor> logger,
            TradingDbContext tradingDbContext,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _tradingDbContext = tradingDbContext;
            var connectionString = configuration["ServiceBusConnectionString"];
            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender("CREATE_ORDER_QUEUE");
        }

        [Function("ScheduledOutboxMessageProcessor")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("ScheduledOutboxMessageProcessor triggered at: {TriggerTime}",
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

            _logger.LogInformation("QuarantinePhase | Found {Count} exhausted messages",
                exhaustedOutboxMessages.Count);

            foreach (var exOutboxMsg in exhaustedOutboxMessages)
            {
                Guid? clientOrderId = Guid.TryParse(exOutboxMsg.Payload, out var parsed) ? parsed : null;

                _logger.LogWarning(
                    "QuarantiningMessage | CorrelationId: {CorrelationId} | OutboxId: {OutboxId} | Reason: {Reason} | RetryCount: {Count}",
                    exOutboxMsg.CorrelationId, exOutboxMsg.Id, exOutboxMsg.RetryReason, exOutboxMsg.RetryCount);

                _tradingDbContext.QuarantinedOutboxMessages.Add(new QuarantinedOutboxMessage
                {
                    Id = Guid.NewGuid(),
                    OriginalOutboxMessageId = exOutboxMsg.Id,
                    ClientOrderId = clientOrderId,
                    Payload = exOutboxMsg.Payload,
                    Reason = exOutboxMsg.RetryReason,
                    FinalRetryCount = exOutboxMsg.RetryCount,
                    QuarantinedAt = DateTimeOffset.UtcNow,
                    ErrorMessage = exOutboxMsg.LastError,
                    CorrelationId = exOutboxMsg.CorrelationId 
                });

                exOutboxMsg.ProcessedAt = DateTimeOffset.UtcNow;
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

            _logger.LogInformation("ProcessingPhase | Found {Count} pending messages",
                outboxMessages.Count);

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
                            _logger.LogInformation(
                                "OrderAlreadyProcessed | CorrelationId: {CorrelationId} | OutboxId: {OutboxId} | ClientOrderId: {ClientOrderId}",
                                outboxMessage.CorrelationId, outboxMessage.Id, clientOrderId);

                            outboxMessage.ProcessedAt = DateTimeOffset.UtcNow;
                            continue;
                        }

                        _logger.LogInformation(
                            "SendingToServiceBus | CorrelationId: {CorrelationId} | OutboxId: {OutboxId} | ClientOrderId: {ClientOrderId}",
                            outboxMessage.CorrelationId, outboxMessage.Id, clientOrderId);

                        await NotifyServiceBusCreateOrderQueue(clientOrderId, outboxMessage.CorrelationId);
                        outboxMessage.ProcessedAt = DateTimeOffset.UtcNow;
                        isServiceBusHealthy = true;

                        _logger.LogInformation(
                            "SentToServiceBus | CorrelationId: {CorrelationId} | OutboxId: {OutboxId} | Queue: CREATE_ORDER_QUEUE",
                            outboxMessage.CorrelationId, outboxMessage.Id);
                    }
                    else
                    {
                        _logger.LogError(
                           "InvalidPayload | CorrelationId: {CorrelationId} | OutboxId: {OutboxId} | Payload: {Payload}",
                            outboxMessage.CorrelationId, outboxMessage.Id, outboxMessage.Payload);

                        outboxMessage.RetryCount++;
                        outboxMessage.RetryReason = OutboxRetryReason.InvalidPayload;
                    }
                }
                catch (ServiceBusException serviceBusException)
                {
                    _logger.LogError(serviceBusException,
                        "ServiceBusError | CorrelationId: {CorrelationId} | OutboxId: {OutboxId} | Error: {Message}",
                        outboxMessage.CorrelationId, outboxMessage.Id, serviceBusException.Message);

                    outboxMessage.RetryCount++;
                    outboxMessage.RetryReason = OutboxRetryReason.ServiceBusUnavailable;
                    outboxMessage.LastError = serviceBusException.Message;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "OutboxProcessingFailed | CorrelationId: {CorrelationId} | OutboxId: {OutboxId}",
                        outboxMessage.CorrelationId, outboxMessage.Id);

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

            _logger.LogInformation("AutoRecoveryPhase | Found {Count} resurrection candidates",
                resurrectCandidates.Count);

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
                    _logger.LogInformation(
                        "ResurrectingMessage | CorrelationId: {CorrelationId} | OutboxId: {OutboxId} | QuarantinedId: {QuarantinedId}",
                        candidate.CorrelationId, originalOutboxMessage.Id, candidate.Id);

                    originalOutboxMessage.ProcessedAt = null;
                    originalOutboxMessage.RetryCount = 4;
                    originalOutboxMessage.RetryReason = OutboxRetryReason.None;

                    candidate.IsResurrected = true;
                    candidate.ResurrectedAt = DateTimeOffset.UtcNow;
                    candidate.ResolutionNotes = "Auto-resurrected: Service Bus connectivity restored";
                }
            }

            _logger.LogInformation(
                "AutoRecoveryComplete | Resurrected {Count} messages",
                resurrectCandidates.Count);
        }

        private async Task NotifyServiceBusCreateOrderQueue(Guid clientOrderId, string correlationId)
        {
            var payload = new { ClientOrderId = clientOrderId };

            var serializedPayload = JsonSerializer.Serialize(payload);

            var message = new ServiceBusMessage(serializedPayload)
            {
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = correlationId
            };

            await _sender.SendMessageAsync(message);
        }
    }
}