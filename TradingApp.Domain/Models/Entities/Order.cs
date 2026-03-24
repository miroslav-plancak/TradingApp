using System;
using TradingApp.Domain.Models.Enums;

namespace TradingApp.Domain.Models.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid ClientOrderId { get; set; }
        public OrderStatus Status { get; set; }
        public int Quantity { get; set; }   
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
