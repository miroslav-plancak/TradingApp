using System;
using TradingApp.Domain.Models.Enums;

namespace TradingApp.Business.DTOs
{
    public class CreateOrderResponseDTO
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
