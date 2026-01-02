using ECommerce.Shared.Kafka.Contracts;
using ECommerce.Shared.Kafka.Interfaces;
using System.Text.Json;

namespace ECommerce.UserService.Application.Services
{
    public sealed class OrderCreatedEventHandler : IKafkaEventHandler
    {
        private readonly ILogger<OrderCreatedEventHandler> _logger;

        public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task HandleAsync(string key, string raw, CancellationToken ct)
        {
            var kafkaEvent = JsonSerializer.Deserialize<KafkaEvent<OrderCreatedEventV1>>(raw);
            if (kafkaEvent?.Data is null)
            {
                _logger.LogWarning("Invalid OrderCreated event payload. Key={Key}", key);
                return Task.CompletedTask;
            }

            _logger.LogInformation("Received OrderCreated: OrderId={OrderId} UserId={UserId} Product={Product} Qty={Qty} Price={Price}",
                kafkaEvent.Data.OrderId, kafkaEvent.Data.UserId, kafkaEvent.Data.Product, kafkaEvent.Data.Quantity, kafkaEvent.Data.Price);

            return Task.CompletedTask;
        }
    }
}
