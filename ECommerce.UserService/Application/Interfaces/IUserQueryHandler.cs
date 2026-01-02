using ECommerce.UserService.Domain;

namespace ECommerce.UserService.Application.Interfaces
{
    public interface IUserQueryHandler
    {
        Task<User> AddUserAsync(string name, string email, CancellationToken ct);
        Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken ct);
        Task<User> GetUserByIdAsync(Guid userid, CancellationToken ct);
    }
}
