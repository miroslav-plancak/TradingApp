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
            //OrderMapper.GetStatusDisplayName(orderRequest.Status);
            await _orderRepository.CreateOrderAsync(orderEntity);
            LogExitWithScope();
            return  new CreateOrderResponseDTO() { };
        }

        public Task GetOrderByIdAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task GetOrdersAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
