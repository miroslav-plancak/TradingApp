using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using TradingApp.Business.DTOs.Order;
using TradingApp.Business.Extensions;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Repositories;
using TradingApp.Business.Interfaces.Services;
using TradingApp.Business.Mappers;
using TradingApp.Domain;
using TradingApp.Domain.Models.Entities.OutboxMessage;

namespace TradingApp.Business.Services.Regular
{
    public class OrderService : TradingAppBaseLoggerExtension<OrderService>,IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly TradingDbContext _tradingDbContext;

        public  OrderService(ITradingAppLogger logger, TradingDbContext tradingDbContext, IOrderRepository orderRepository) : base(logger)
        {
            _tradingDbContext = tradingDbContext;
            _orderRepository = orderRepository;
        }

        public async Task<CreatedOrderResponseDTO> CreateOrderAsync(CreateOrderRequestDTO orderRequest)
        {
            LogEntryWithScope();

            using var transaction = await _tradingDbContext.Database.BeginTransactionAsync();

            var orderEntityRequest = OrderMapper.ToEntity(orderRequest);
            var order = await _orderRepository.CreateOrderAsync(orderEntityRequest);

            _tradingDbContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "OrderCreated",
                Payload =  order.ClientOrderId.ToString(),
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _tradingDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            LogExitWithScope();

            var createdOrderResponseDTO = OrderMapper.ToCreatedOrderResponseDTO(order);

            return createdOrderResponseDTO;
        }

        public async Task<OrderResponseDTO> GetOrderByIdAsync(Guid orderId)
        {
            LogEntryWithScope();

            var orderEntity = await _orderRepository.GetOrderByIdAsync(orderId);

            if (orderEntity == null)
            {
                throw new KeyNotFoundException($"Order {orderId} not found.");
            }

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

        public async Task<bool> DeleteOrderAsync(Guid orderId)
        {
            LogEntryWithScope();

            var orderEntity = await _orderRepository.GetOrderByIdAsync(orderId);

            if (orderEntity == null)
            {
                throw new KeyNotFoundException($"Order {orderId} not found.");
            }

            var deleted = await _orderRepository.DeleteOrderAsync(orderId);

            LogExitWithScope();

            return deleted;
        }

        public async Task<int> DeleteAllOrdersAsync()
        {
            LogEntryWithScope();

            var deletedCount = await _orderRepository.DeleteAllOrdersAsync();

            LogExitWithScope();

            return deletedCount;
        }
    }
}
