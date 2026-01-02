using ECommerce.OrderService.Application.Interfaces;
using ECommerce.OrderService.Domain;
using ECommerce.Shared.Exceptions;

namespace ECommerce.OrderService.Application.Services
{
    public sealed class OrderQueryHandler : IOrderQueryHandler
    {
        private readonly IOrderRepository _orderRepository;

        public OrderQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Order> AddOrderAsync(Guid userId, string product, int quantity, decimal price, CancellationToken ct)
        {
            // service validation??

            var order = new Order(userId, product, quantity, price);
            await _orderRepository.AddAsync(order, ct);
            await _orderRepository.SaveChangesAsync(ct);

            return order;
        }

        public async Task<IReadOnlyList<Order>> GetAllOrdersByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var orders = await _orderRepository.GetAllByUserIdAsync(userId, ct);
            return orders;
        }

        public async Task<Order> GetOrderByIdAsync(Guid orderId, CancellationToken ct)
        {
            var order = await _orderRepository.GetByIdAsync(orderId, ct);
            if (order is null)
            {
                throw new NotFoundException($"Order {orderId} not found");
            }
            return order;
        }
    }
}