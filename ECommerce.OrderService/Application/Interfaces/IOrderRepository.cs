using ECommerce.OrderService.Domain;

namespace ECommerce.OrderService.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order, CancellationToken ct);
        Task<IReadOnlyList<Order>> GetAllByUserIdAsync(Guid userId, CancellationToken ct);
        Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct);

        Task SaveChangesAsync(CancellationToken ct);
    }
}
