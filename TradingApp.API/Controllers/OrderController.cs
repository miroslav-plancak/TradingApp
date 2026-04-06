using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TradingApp.Business.DTOs;
using TradingApp.Business.Interfaces.Logger;
using TradingApp.Business.Interfaces.Services;

namespace TradingApp.API.Controllers
{
    public class OrderController : TradingAppBaseController<OrderController>
    {
        private readonly IOrderService _orderService;
        public OrderController(ITradingAppLogger logger, IOrderService orderService): base(logger)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<ActionResult> CreateOrderAsync(CreateOrderRequestDTO createOrder)
        {
            var result = await _orderService.CreateOrderAsync(createOrder);
            return Ok(new { Message = "Order created mock!"});
        }

        [HttpGet]
        public async Task<ActionResult> GetOrdersAsync()
        {
            await Task.CompletedTask;
            return Ok();
        }

        [HttpGet("order/{oderId}")]
        public async Task<ActionResult> GetOrderByIdAsync([FromBody] string orderId)
        {
            await Task.CompletedTask;
            return Ok();
        }
    }
}
