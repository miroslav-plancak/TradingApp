using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using TradingApp.Business.Constants;
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

            if(order == null)
            {
                throw new Exception("Order creation failed.");
            }

            var orderDTO = OrderMapper.ToCreatedOrderResponseDTO(order);
            
            await NotifyServiceBusCreateOrderQueue(order.ClientOrderId);

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

        private async Task NotifyServiceBusCreateOrderQueue(Guid clientOrderId) 
        {
            await using var client = new ServiceBusClient(AzureFunctionConstants.ConnectionString);
            ServiceBusSender sender = client.CreateSender("CREATE_ORDER_QUEUE");

            var payload = new { ClientOrderId = clientOrderId };
            var json = JsonSerializer.Serialize(payload);

            await sender.SendMessageAsync(new ServiceBusMessage(json));
        }
    }
}
