using System;
using TradingApp.Domain.Models.Enums;

namespace TradingApp.Domain.Models.Entities
{
    public class OrderResponse
    {
        public Guid ClientOrderId {get;set;}
        public OrderStatus Status { get; set; }
    }
}
