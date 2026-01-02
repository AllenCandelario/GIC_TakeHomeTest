using ECommerce.UserService.Application.Interfaces;
using ECommerce.UserService.Domain;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace ECommerce.UserService.Infrastructure.Persistence
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly UserDbContext _dbContext;

        public UserRepository(UserDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(User user, CancellationToken ct)
        {
            await _dbContext.Users.AddAsync(user, ct);
        }

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct)
        {
            return await _dbContext.Users.ToListAsync(ct);
        }

        public async Task<User?> GetByIdAsync(Guid userId, CancellationToken ct)
        {
            return await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        {
            return await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            return _dbContext.SaveChangesAsync();
        }

    }
}
