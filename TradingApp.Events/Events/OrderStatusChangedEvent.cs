namespace TradingApp.Events.Events
{
    public class OrderStatusChangedEvent
    {
        public Guid ClientOrderId { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public int Sequence { get; set; }
    }
}
