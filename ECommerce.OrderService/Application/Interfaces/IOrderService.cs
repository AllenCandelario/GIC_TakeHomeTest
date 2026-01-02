using ECommerce.OrderService.Domain;

namespace ECommerce.OrderService.Application.Interfaces
{
    public interface IOrderService
    {
        Task<Order> AddOrderAsync(Guid userId, string product, int quantity, decimal price, CancellationToken ct);
        Task<IReadOnlyList<Order>> GetAllOrdersByUserIdAsync(Guid userId, CancellationToken ct);
        Task<Order> GetOrderByIdAsync(Guid orderId, CancellationToken ct);
    }
}
