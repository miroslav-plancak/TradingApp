using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TradingApp.Business.DTOs;
using TradingApp.Business.Extensions;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Repositories;
using TradingApp.Business.Interfaces.Services;
using TradingApp.Business.Mappers;

namespace TradingApp.Business.Services.Regular
{
    public class OrderService : TradingAppBaseLoggerExtension<OrderService>,IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        public  OrderService(ITradingAppLogger logger, IOrderRepository orderRepository) : base(logger)
        {
            _orderRepository = orderRepository;
        }

        public async Task<CreateOrderResponseDTO> CreateOrderAsync(CreateOrderRequestDTO orderRequest)
        {
            LogEntryWithScope();

            var orderEntityRequest = OrderMapper.ToEntity(orderRequest);
            var order = await _orderRepository.CreateOrderAsync(orderEntityRequest);
            var orderDTO = OrderMapper.ToCreatedOrderResponseDTO(order);

            //triger az OrderExecutionProvider func
            await NotifyOrderExecutionProviderAsync(order.ClientOrderId);

            LogExitWithScope();
            return orderDTO;
        }

        public async Task<OrderResponseDTO> GetOrderByIdAsync(Guid orderId)
        {
            LogEntryWithScope();

            var orderEntity = await _orderRepository.GetOrderByIdAsync(orderId);
            var orderDTO = OrderMapper.ToOrderResponseDTO(orderEntity);

            LogExitWithScope();
            return orderDTO;
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetOrdersAsync()
        {
            LogEntryWithScope();

            var orderEntities = await _orderRepository.GetOrdersAsync();
            var orderDTOs = OrderMapper.ToOrderResponseDTOs(orderEntities);

            LogExitWithScope();

            return orderDTOs;
        }

        private async Task NotifyOrderExecutionProviderAsync(Guid clientOderId) 
        {
            var payload = new { ClientOrderId = clientOderId };
            var json = JsonSerializer.Serialize(payload);

            using var client = new HttpClient();

            var response = await client.PostAsync(
                "http://localhost:7174/api/OrderExecutionProvider",
                new StringContent(json, Encoding.UTF8, "application/json")
                );
            response.EnsureSuccessStatusCode();
        }
    }
}
