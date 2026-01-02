
namespace ECommerce.Shared.Kafka.Interfaces
{
    public interface IKafkaEventHandler
    {
        Task HandleAsync(string key, string rawJson, CancellationToken ct);
    }
}
