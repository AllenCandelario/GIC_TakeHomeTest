using ECommerce.OrderService.API.DTOs;
using ECommerce.OrderService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Security.Principal;

namespace ECommerce.OrderService.API.Controllers
{
    [ApiController]
    [Route("api/v1/orders")]

    public sealed class OrderController : ControllerBase
    {
        private readonly IOrderQueryHandler _queryHandler;

        public OrderController(IOrderQueryHandler queryHandler)
        {
            _queryHandler = queryHandler;
        }

        [HttpPost("")]
        public async Task<ActionResult<OrderResponseDto>> AddOrderAsync([FromBody] OrderRequestDto orderDto, CancellationToken ct)
        {
            var order = await _queryHandler.AddOrderAsync(orderDto.UserId, orderDto.Product, orderDto.Quantity, orderDto.Price, ct);
            var response = new OrderResponseDto(order.Id, order.UserId, order.Product, order.Quantity, order.Price, order.CreatedAtUtc);
            return Created($"/api/v1/orders/{order.Id}", response);
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<ActionResult<List<OrderResponseDto>>> GetAllOrdersByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var orders = await _queryHandler.GetAllOrdersByUserIdAsync(userId, ct);
            var response = orders.Select(o => new OrderResponseDto(o.Id, o.UserId, o.Product, o.Quantity, o.Price, o.CreatedAtUtc)).ToList();
            return Ok(response); ;
        }

        [HttpGet("{orderId:guid}")]
        public async Task<ActionResult<OrderResponseDto>> GetOrderByIdAsync(Guid orderId, CancellationToken ct)
        {
            var order = await _queryHandler.GetOrderByIdAsync(orderId, ct);
            var response = new OrderResponseDto(order.Id, order.UserId, order.Product, order.Quantity, order.Price, order.CreatedAtUtc);
            return Ok(response);
        }
    }
}