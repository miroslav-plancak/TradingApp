using System;

namespace TradingApp.Domain.Models.Entities
{
    /// <summary>
    /// Tracks messages that ended up in the Dead Letter Queue
    /// Used for monitoring, auditing, and manual intervention
    /// </summary>
    public class DeadLetterLog
    {
        /// <summary>
        /// Unique identifier for this dead letter log entry
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The ClientOrderId from the original order that failed
        /// </summary>
        public Guid ClientOrderId { get; set; }

        /// <summary>
        /// The raw message body from Service Bus
        /// </summary>
        public string MessageBody { get; set; }

        /// <summary>
        /// Reason why the message was dead-lettered
        /// e.g., "Max retries exceeded", "Order not found", "Processing exception"
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// When this message was dead-lettered
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Whether this issue has been manually resolved by ops team
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// Notes from operations team about resolution
        /// </summary>
        public string ResolutionNotes { get; set; }

        /// <summary>
        /// When the issue was resolved
        /// </summary>
        public DateTimeOffset? ResolvedAt { get; set; }

        /// <summary>
        /// Who resolved the issue (user ID or email)
        /// </summary>
        public string ResolvedBy { get; set; }
    }
}