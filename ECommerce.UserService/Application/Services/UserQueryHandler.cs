using ECommerce.UserService.Application.Interfaces;
using ECommerce.UserService.Domain;
using ECommerce.Shared.Exceptions;

namespace ECommerce.UserService.Application.Services
{
    public sealed class UserQueryHandler : IUserQueryHandler
    {
        private readonly IUserRepository _userRepository;

        public UserQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> AddUserAsync(string name, string email, CancellationToken ct)
        {
            // service validation - existing email
            var userEmailCheck = await _userRepository.GetByEmailAsync(email, ct);
            if (userEmailCheck is not null)
            {
                throw new ConflictException("Email already exists");
            }

            var user = new User(name, email);
            await _userRepository.AddAsync(user, ct);
            await _userRepository.SaveChangesAsync(ct);

            return user;
        }

        public async Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken ct)
        {
            var users = await _userRepository.GetAllAsync(ct);
            return users;
        }

        public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct)
        {
            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user is null)
            {
                throw new NotFoundException($"User {userId} not found");
            }
            return user;
        }
    }
}
