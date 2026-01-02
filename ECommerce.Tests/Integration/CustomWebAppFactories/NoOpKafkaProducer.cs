using ECommerce.Shared.Kafka.Contracts;
using ECommerce.Shared.Kafka.Interfaces;

namespace ECommerce.Tests.Integration.CustomWebAppFactories
{
    public sealed class NoOpKafkaProducer : IKafkaProducer
    {
        public Task PublishAsync<T>(string topic, string key, KafkaEvent<T> kafkaEvent, CancellationToken ct) => Task.CompletedTask;
    }
}
