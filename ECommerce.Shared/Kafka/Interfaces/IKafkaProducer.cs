using ECommerce.Shared.Kafka.Contracts;

namespace ECommerce.Shared.Kafka.Interfaces
{
    public interface IKafkaProducer
    {
        Task PublishAsync<T>(string topic, string key, KafkaEvent<T> kafkaEvent, CancellationToken ct);
    }
}
