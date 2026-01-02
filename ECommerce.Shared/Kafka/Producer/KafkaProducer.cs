using Confluent.Kafka;
using ECommerce.Shared.Kafka.Contracts;
using ECommerce.Shared.Kafka.Interfaces;
using ECommerce.Shared.Kafka.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ECommerce.Shared.Kafka.Producer
{
    public sealed class KafkaProducer : IKafkaProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;

        public KafkaProducer(IOptions<KafkaProducerConfigOptions> options)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = options.Value.BootstrapServers,
                ClientId = options.Value.ClientId,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = 3,
                RetryBackoffMaxMs = 100,
                LingerMs = 5,
                MessageTimeoutMs = 1000,       
                RequestTimeoutMs = 800,        
                SocketTimeoutMs = 800
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task PublishAsync<T>(string topic, string key, KafkaEvent<T> kafkaEvent, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(kafkaEvent);
            var msg = new Message<string, string> { Key = key, Value = json };
            await _producer.ProduceAsync(topic, msg, ct);
        }

        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(3));
            _producer.Dispose();
        }

    }
}
