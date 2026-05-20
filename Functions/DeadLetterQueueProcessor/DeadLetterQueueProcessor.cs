using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Business.Interfaces.Services;
using TradingApp.Domain;
using TradingApp.Domain.Models.Enums;
using TradingApp.Events.Payloads;

namespace DeadLetterQueueProcessor
{
    public class DeadLetterQueueProcessor
    {
        private readonly ILogger<DeadLetterQueueProcessor> _logger;
        private readonly TradingDbContext _tradingDbContext;
        private readonly IDeadLetterService _deadLetterService;

        public DeadLetterQueueProcessor
        (
            ILogger<DeadLetterQueueProcessor> logger,
            TradingDbContext tradingDbContext,
            IDeadLetterService deadLetterService
        )
        {
            _logger = logger;
            _tradingDbContext = tradingDbContext;
            _deadLetterService = deadLetterService;
        }

        [Function("DeadLetterQueueProcessor")]
        public async Task Run
        (
            [ServiceBusTrigger(
                queueName: "CREATE_ORDER_QUEUE/$DeadLetterQueue",
                Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions
        )
        {
            var correlationId = message.CorrelationId ?? "UNKNOWN";

            _logger.LogWarning(
                "DeadLetterMessageReceived | CorrelationId: {CorrelationId} | MessageId: {MessageId} | Time: {Time}",
                correlationId, message.MessageId, DateTimeOffset.UtcNow);

            try
            {
                var payload = JsonSerializer.Deserialize<OrderPayload>(message.Body.ToString());

                if (payload == null)
                {
                    _logger.LogError(
                        "DeadLetterDeserializationFailed | CorrelationId: {CorrelationId} | MessageBody: {MessageBody}",
                        correlationId, message.Body.ToString());
                    return;
                }

                _logger.LogWarning(
                    "ProcessingDeadLetter | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId}",
                    correlationId, payload.ClientOrderId);

                var order = await _tradingDbContext.Orders
                    .FirstOrDefaultAsync(x => x.ClientOrderId == payload.ClientOrderId);

                if (order == null)
                {
                    _logger.LogError(
                        "OrderNotFoundInDatabase | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId}",
                        correlationId, payload.ClientOrderId);

                    await _deadLetterService.CreateDeadLetterLogAsync(
                        message.Body.ToString(),
                        payload.ClientOrderId,
                        "Order not found in the database.",
                        correlationId);

                    return;
                }

                if (order.IsProcessed)
                {
                    _logger.LogInformation(
                        "OrderAlreadyProcessed | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId} | Status: {Status}",
                        correlationId, payload.ClientOrderId, order.Status);

                    await _deadLetterService.MarkOutboxMessageAsProcessedAsync(payload.ClientOrderId);
                    return;
                }

                _logger.LogError(
                    "OrderFailedAndInDLQ | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId}",
                    correlationId, payload.ClientOrderId);

                order.Status = OrderStatus.REJECTED;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                order.IsProcessed = true;
                await _tradingDbContext.SaveChangesAsync();

                await _deadLetterService.MarkOutboxMessageAsProcessedAsync(payload.ClientOrderId);

                await _deadLetterService.CreateDeadLetterLogAsync(
                    message.Body.ToString(),
                    payload.ClientOrderId,
                    "Max retries exceeded",
                    correlationId);

                await SendAlertToOpsTeam(payload.ClientOrderId, correlationId);

                _logger.LogInformation(
                    "DeadLetterProcessed | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId} | Status: REJECTED",
                    correlationId, payload.ClientOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "DeadLetterProcessingFailed | CorrelationId: {CorrelationId} | MessageBody: {MessageBody}",
                    correlationId, message.Body.ToString());

                throw;
            }
        }

        private async Task SendAlertToOpsTeam(Guid clientOrderId, string correlationId)
        {
            _logger.LogWarning(
                "DeadLetterAlertSent | CorrelationId: {CorrelationId} | ClientOrderId: {ClientOrderId}",
                correlationId, clientOrderId);

            await Task.CompletedTask;
        }
    }
}