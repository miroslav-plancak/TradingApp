using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingApp.Events.Events;

namespace RiskAnalysisProcessor
{
    public class RiskAnalysisProcessor
    {
        private readonly ILogger<RiskAnalysisProcessor> _logger;

        public RiskAnalysisProcessor(ILogger<RiskAnalysisProcessor> logger)
        {
            _logger = logger;
        }

        [Function(nameof(RiskAnalysisProcessor))]
        public async Task Run
        (
            [ServiceBusTrigger(
            "order_events_topic",
            "risk-analysis", 
            Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message
        )
        {
            var correlationId = message.CorrelationId ?? "CorrelationId";

            _logger.LogInformation("RiskAnalysisProcessor started | CorrelationId: {CorrelationId}",
                        correlationId);

            var orderEvent = JsonSerializer.Deserialize<OrderProcessedEvent>(message.Body.ToString());

            if (orderEvent == null)
            {
                _logger.LogWarning("OrderEventNull | CorrelationId: {CorrelationId}", correlationId);
                return;
            }

            _logger.LogInformation("Analyzing risk for Order with CorrelationId: {CorrelationId} | ClientOrderId {ClientOrderId}",
                correlationId, orderEvent.ClientOrderId);

            var riskScore = await CalculateRiskScore(orderEvent);

            _logger.LogInformation(
                "Risk analysis complete with CorrelationId {CorrelationId} " +
                "| ClientOrderId: {ClientOrderId} | RiskScore: {RiskScore}",
                correlationId,
                orderEvent.ClientOrderId,
                riskScore);
        }

        private async Task<double> CalculateRiskScore(OrderProcessedEvent orderEvent)
        {
            await Task.Delay(500);
            var random = new Random();
            return random.NextDouble() * 100;
        }
    }
}