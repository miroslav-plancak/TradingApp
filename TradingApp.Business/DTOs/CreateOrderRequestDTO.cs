using System;
using TradingApp.Domain.Models.Enums;

namespace TradingApp.Business.DTOs
{
    public class CreateOrderRequestDTO
    {
        public OrderStatus Status { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
