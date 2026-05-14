namespace TradingApp.Events.Events
{
    public class OrderProcessedEvent
    {
        public Guid ClientOrderId { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
    }
}