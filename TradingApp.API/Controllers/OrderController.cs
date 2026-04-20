using Microsoft.AspNetCore.Mvc;
using System;
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
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult> GetOrdersAsync()
        {
            var result = await _orderService.GetOrdersAsync();
            return Ok(result);
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult> GetOrderByIdAsync([FromRoute] Guid orderId)
        {
            var result = await _orderService.GetOrderByIdAsync(orderId);
            return Ok(result);
        }
    }
}
