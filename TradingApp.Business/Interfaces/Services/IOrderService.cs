using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingApp.Business.DTOs;

namespace TradingApp.Business.Interfaces.Services
{
    public interface IOrderService
    {
        Task<CreateOrderResponseDTO> CreateOrderAsync(CreateOrderRequestDTO createOrder);
        Task <IEnumerable<OrderResponseDTO>> GetOrdersAsync();
        Task <OrderResponseDTO> GetOrderByIdAsync(Guid orderId);
    }
}
