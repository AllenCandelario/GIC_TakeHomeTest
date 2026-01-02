using ECommerce.UserService.Domain;

namespace ECommerce.UserService.Application.Interfaces
{
    public interface IUserRepository
    {
        Task AddAsync(User user, CancellationToken ct);
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct); // Maybe for UI
        Task<User?> GetByIdAsync(Guid userId, CancellationToken ct);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct); // For validation

        Task SaveChangesAsync(CancellationToken ct);
    }
}
