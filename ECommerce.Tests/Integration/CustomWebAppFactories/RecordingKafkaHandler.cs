
using ECommerce.Shared.Kafka.Interfaces;
using System.Collections.Concurrent;

namespace ECommerce.Tests.Integration.CustomWebAppFactories
{
    public sealed class RecordingKafkaHandler : IKafkaEventHandler
    {
        public ConcurrentQueue<(string Key, string Raw)> Seen { get; } = new();

        public Task HandleAsync(string key, string rawJson, CancellationToken ct)
        {
            Seen.Enqueue((key, rawJson));
            return Task.CompletedTask;
        }
    }
}
