namespace ECommerce.Shared.Kafka.Contracts
{
    public sealed record KafkaEvent<T>(
        Guid EventId,
        string EventType, // corresponds to topic for now
        int Version, 
        DateTime OccurredAtUtc,
        string Producer,
        string Key,
        T Data, 
        string? TraceId = null
    );
}
