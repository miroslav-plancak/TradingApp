using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RiskAnalysisProcessor;

public class RiskAnalysisProcessor
{
    private readonly ILogger<RiskAnalysisProcessor> _logger;

    public RiskAnalysisProcessor(ILogger<RiskAnalysisProcessor> logger)
    {
        _logger = logger;
    }

    [Function(nameof(RiskAnalysisProcessor))]
    public async Task Run(
        [ServiceBusTrigger(
        "ORDER_EVENTS_TOPIC",
        "risk-analysis", 
        Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message
    )
    {
        _logger.LogInformation("RiskAnalysisFunction triggered.");

        var orderEvent = JsonSerializer.Deserialize<OrderProcessedEvent>(message.Body);

        if (orderEvent == null)
        {
            _logger.LogWarning("Received null orderEvent.");
            return;
        }

        _logger.LogInformation(
            "Analyzing risk for Order: {ClientOrderId}, Status: {Status}",
            orderEvent.ClientOrderId,
            orderEvent.Status);


        var riskScore = await CalculateRiskScore(orderEvent);

        _logger.LogInformation(
            "Risk analysis complete. ClientOrderId: {ClientOrderId}, RiskScore: {RiskScore}",
            orderEvent.ClientOrderId,
            riskScore);
    }
    private async Task<double> CalculateRiskScore(OrderProcessedEvent orderEvent)
    {
        await Task.Delay(500);
        var random = new Random();
        return random.NextDouble() * 100;
    }

    public class OrderProcessedEvent
    {
        public Guid ClientOrderId { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
    }
}