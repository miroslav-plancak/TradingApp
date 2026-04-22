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
using TradingApp.Domain.Models.Entities;

namespace TradingApp.Business.Services.Regular
{
    public class OrderService : TradingAppBaseLoggerExtension<OrderService>,IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly JsonSerializerOptions _jsonOptions;

        public  OrderService(ITradingAppLogger logger, IOrderRepository orderRepository) : base(logger)
        {
            _orderRepository = orderRepository;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<CreatedOrderResponseDTO> CreateOrderAsync(CreateOrderRequestDTO orderRequest)
        {
            LogEntryWithScope();

            var orderEntityRequest = OrderMapper.ToEntity(orderRequest);
            var order = await _orderRepository.CreateOrderAsync(orderEntityRequest);
            var orderDTO = OrderMapper.ToCreatedOrderResponseDTO(order);

            var funcOrderResponse = await NotifyOrderExecutionProviderAsync(order.ClientOrderId);

            if (funcOrderResponse != null)
            {
                orderDTO.Status = funcOrderResponse.Status.ToString();
            }

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

        private async Task<OrderResponse> NotifyOrderExecutionProviderAsync(Guid clientOrderId) 
        {
            var payload = new { ClientOrderId = clientOrderId };
            var json = JsonSerializer.Serialize(payload);

            using var client = new HttpClient();

            var response = await client.PostAsync(
                "http://localhost:7174/api/OrderExecutionProvider",
                new StringContent(json, Encoding.UTF8, "application/json")
                );

            var responseContent = await response.Content.ReadAsStringAsync() ;
            var result = JsonSerializer.Deserialize<OrderResponse>(responseContent, _jsonOptions);

            return result;
        }
    }
}
