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

        public DeadLetterQueueProcessor(
            ILogger<DeadLetterQueueProcessor> logger,
            TradingDbContext tradingDbContext,
            IDeadLetterService deadLetterService)
        {
            _logger = logger;
            _tradingDbContext = tradingDbContext;
            _deadLetterService = deadLetterService;
        }

        [Function("DeadLetterQueueProcessor")]
        public async Task Run(
            [ServiceBusTrigger(
            queueName: "CREATE_ORDER_QUEUE/$DeadLetterQueue",
            Connection = "ServiceBusConnection")]
        string messageBody)
        {
            _logger.LogWarning("Dead Letter Queue message received at: {Time}", DateTimeOffset.UtcNow);

            try
            {
                var payload = JsonSerializer.Deserialize<OrderPayload>(messageBody);

                if (payload == null)
                {
                    _logger.LogError("Failed to deserialize dead letter message: {MessageBody}", messageBody);
                    return;
                }

                _logger.LogWarning(
                    "Processing dead letter for Order ClientOrderId: {ClientOrderId}",
                    payload.ClientOrderId);

                var order = await _tradingDbContext.Orders
                    .FirstOrDefaultAsync(x => x.ClientOrderId == payload.ClientOrderId);

                if (order == null)
                {
                    _logger.LogError(
                        "Order not found in database for dead letter message. ClientOrderId: {ClientOrderId}",
                        payload.ClientOrderId);

                    await _deadLetterService.CreateDeadLetterLogAsync(
                        messageBody,
                        payload.ClientOrderId,
                        "Order not found in the database.");

                    return;
                }

                if (order.IsProcessed)
                {
                    _logger.LogInformation(
                        "Order already processed, dead letter can be ignored. ClientOrderId: {ClientOrderId}, Status: {Status}",
                        payload.ClientOrderId,
                        order.Status);

                    await _deadLetterService.MarkOutboxMessageAsProcessedAsync(payload.ClientOrderId);
                    return;
                }

                _logger.LogError(
                    "Order failed to process and ended up in DLQ. ClientOrderId: {ClientOrderId}",
                    payload.ClientOrderId);

                order.Status = OrderStatus.REJECTED;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                order.IsProcessed = true;
                await _tradingDbContext.SaveChangesAsync();

                await _deadLetterService.MarkOutboxMessageAsProcessedAsync(payload.ClientOrderId);

                await _deadLetterService.CreateDeadLetterLogAsync(
                    messageBody,
                    payload.ClientOrderId,
                    "Max retries exceeded");

                await SendAlertToOpsTeam(payload.ClientOrderId);

                _logger.LogInformation(
                    "Dead letter message processed. Order marked as REJECTED. ClientOrderId: {ClientOrderId}",
                    payload.ClientOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing dead letter message: {MessageBody}",
                    messageBody);

                throw;
            }
        }

        private async Task SendAlertToOpsTeam(Guid clientOrderId)
        {
            _logger.LogWarning(
                "Dead letter detected - ClientOrderId: {ClientOrderId}",
                clientOrderId
                );

            await Task.CompletedTask;
        }
    }
}
