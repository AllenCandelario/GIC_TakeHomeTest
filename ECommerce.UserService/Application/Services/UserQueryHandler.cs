using ECommerce.UserService.Application.Interfaces;
using ECommerce.UserService.Domain;
using ECommerce.Shared.Exceptions;
using ECommerce.Shared.Kafka.Interfaces;
using ECommerce.Shared.Kafka.Contracts;

namespace ECommerce.UserService.Application.Services
{
    public sealed class UserQueryHandler : IUserQueryHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly string _userCreatedTopic;

        public UserQueryHandler(IUserRepository userRepository, IKafkaProducer kafkaProducer, IConfiguration config)
        {
            _userRepository = userRepository;
            _kafkaProducer = kafkaProducer;
            _userCreatedTopic = config["Kafka:Topics:UserCreatedTopic"] ?? throw new InvalidOperationException("Kafka:Topics:UserCreatedTopic missing");
        }

        public async Task<User> AddUserAsync(string name, string email, CancellationToken ct)
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

            // publish message after db write
            var userCreatedEvent = new UserCreatedEventV1(user.Id, user.Name, user.Email, user.CreatedAtUtc);

            var kafkaEvent = new KafkaEvent<UserCreatedEventV1>(
                EventId: Guid.NewGuid(),
                EventType: _userCreatedTopic,
                Version: 1,
                OccurredAtUtc: DateTime.UtcNow,
                Producer: "user-service",
                Key: user.Id.ToString(),
                Data: userCreatedEvent
            );

            try
            {
                await _kafkaProducer.PublishAsync(_userCreatedTopic, kafkaEvent.Key, kafkaEvent, ct);
            }
            catch( Exception ex)
            {
                // dead stop here, no outbox or retry mechanisms for now
                throw new ServiceUnavailableException($"Failed to publish user creation event: {ex.Message}, please retry");
            }

            return user;
        }

        public async Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken ct)
        {
            var users = await _userRepository.GetAllAsync(ct);
            return users;
        }

        public async Task<User> GetUserByIdAsync(Guid userId, CancellationToken ct)
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
