using System;
using System.Collections.Generic;
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

            var orderEntity = OrderMapper.ToEntity(orderRequest);
            var result = await _orderRepository.CreateOrderAsync(orderEntity);
            var orderDTO = OrderMapper.ToCreatedOrderResponseDTO(result);

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
    }
}
