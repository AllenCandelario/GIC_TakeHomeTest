using Confluent.Kafka;

namespace ECommerce.Shared.Kafka.Options
{
    public sealed class KafkaConsumerConfigOptions
    {
        public required string BootstrapServers { get; init; }
        public required string GroupId { get; init; }
        public required string[] Topics { get; init; }

        public AutoOffsetReset AutoOffsetReset { get; init; }
        public bool EnableAutoCommit { get; init; }

        public int MaxPollIntervalMs { get; init; }
        public int SessionTimeoutMs { get; init; }
    }
}
