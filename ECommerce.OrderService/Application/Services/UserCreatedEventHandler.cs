using ECommerce.Shared.Kafka.Contracts;
using ECommerce.Shared.Kafka.Interfaces;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ECommerce.OrderService.Application.Services
{
    public class UserCreatedEventHandler : IKafkaEventHandler
    {
        private readonly ILogger<UserCreatedEventHandler> _logger;

        public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task HandleAsync(string key, string raw, CancellationToken ct)
        {
            KafkaEvent<UserCreatedEventV1>? kafkaEvent;
            try
            {
                kafkaEvent = JsonSerializer.Deserialize<KafkaEvent<UserCreatedEventV1>>(raw);
            }
            catch (JsonException jEx)
            {
                _logger.LogWarning(jEx, "Invalid UserCreated event JSON. Key={Key}", key);
                return Task.CompletedTask;
            }

            if (kafkaEvent?.Data is null)
            {
                _logger.LogWarning("Invalid UserCreated event payload. Key={Key}", key);
                return Task.CompletedTask;
            }

            _logger.LogInformation("Received UserCreated: UserId={UserId} Name={Name} Email={Email}",
                kafkaEvent.Data.UserId, kafkaEvent.Data.Name, kafkaEvent.Data.Email);

            return Task.CompletedTask;
        }
    }
}
