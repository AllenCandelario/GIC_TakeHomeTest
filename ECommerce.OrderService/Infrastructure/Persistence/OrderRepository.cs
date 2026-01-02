using ECommerce.OrderService.Application.Interfaces;
using ECommerce.OrderService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.OrderService.Infrastructure.Persistence
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _dbContext;

        public OrderRepository(OrderDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Order order, CancellationToken ct)
        {
            await _dbContext.Orders.AddAsync(order, ct);
        }

        public async Task<IReadOnlyList<Order>> GetAllByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _dbContext.Orders.Where(o => o.UserId == userId).ToListAsync(ct);
        }

        public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct)
        {
            return await _dbContext.Orders.SingleOrDefaultAsync(o => o.Id == orderId, ct);
        }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            return _dbContext.SaveChangesAsync(ct);
        }
    }
}