using Confluent.Kafka;
using ECommerce.Shared.Kafka.Contracts;
using ECommerce.Tests.Integration.CustomWebAppFactories;
using System.Diagnostics;
using Testcontainers.Kafka;
using System.Text.Json;
using FluentAssertions;
using Confluent.Kafka.Admin;

namespace ECommerce.Tests.Integration.KafkaTests
{
    public sealed class KafkaConsumerIntegrationTests : IAsyncLifetime
    {
        private readonly KafkaContainer _kafka = new KafkaBuilder().Build();
        public async Task InitializeAsync()
        {
            await _kafka.StartAsync();
        }
        public async Task DisposeAsync()
        {
            await _kafka.DisposeAsync();
        }

        [Fact]
        public async Task UserService_ConsumesOrderCreated_Invokes_Handler()
        {
            // Arrange - prep host
            var bootstrap = _kafka.GetBootstrapAddress();
            using var factory = new KafkaEnabledUserServiceFactory(bootstrap);
            using var client = factory.CreateClient(); 

            // prep sample order created event
            var topic = "order.created.v1";
            var evt = new KafkaEvent<OrderCreatedEventV1>(
                EventId: Guid.NewGuid(),
                EventType: topic,
                Version: 1,
                OccurredAtUtc: DateTime.UtcNow,
                Producer: "test",
                Key: Guid.NewGuid().ToString(),
                Data: new OrderCreatedEventV1(
                    OrderId: Guid.NewGuid(),
                    UserId: Guid.NewGuid(),
                    Product: "P",
                    Quantity: 1,
                    Price: 10m,
                    CreatedAtUtc: DateTime.UtcNow
                )
            );

            // Act - produce into kafka 
            using var producer = new ProducerBuilder<string, string>( new ProducerConfig { BootstrapServers = bootstrap }).Build();

            await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = evt.Key,
                Value = JsonSerializer.Serialize(evt)
            });

            // wait up to 5 seconds for consumer to pick it up
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(5))
            {
                if (!factory.RecordingHandler.Seen.IsEmpty) break;
                await Task.Delay(100);
            }

            // Assert
            factory.RecordingHandler.Seen.Should().NotBeEmpty();
            factory.RecordingHandler.Seen.TryPeek(out var seen).Should().BeTrue();
            seen!.Key.Should().Be(evt.Key);
        }

        [Fact]
        public async Task UserService_HandlerThrows_PublishesToDlq()
        {
            // Arrange - prep host and handler to throw 
            var bootstrap = _kafka.GetBootstrapAddress();
            using var factory = new KafkaEnabledUserServiceFactory(bootstrap)
            {
                UseThrowingHandler = true
            };
            using var client = factory.CreateClient();

            // prep sample order created event
            var topic = "order.created.v1";
            var dlqTopic = "user-service.dlq.v1";
            var evt = new KafkaEvent<OrderCreatedEventV1>(
                Guid.NewGuid(), topic, 1, DateTime.UtcNow, "test",
                Key: Guid.NewGuid().ToString(),
                Data: new OrderCreatedEventV1(Guid.NewGuid(), Guid.NewGuid(), "P", 1, 10m, DateTime.UtcNow)
            );

            // Act - produce poison message, should redirect to dlq
            using var producer = new ProducerBuilder<string, string>( new ProducerConfig { BootstrapServers = bootstrap }).Build();
            await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = evt.Key,
                Value = JsonSerializer.Serialize(evt)
            });

            // consume from DLQ
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrap,
                GroupId = "dlq-test-" + Guid.NewGuid(),
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            await EnsureTopicExists(bootstrap, dlqTopic);
            await EnsureTopicExists(bootstrap, topic);

            using var dlqConsumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            dlqConsumer.Subscribe(dlqTopic);
            ConsumeResult<string, string>? dlqMsg = null;

            // wait up to 5 seconds for consumer to pick it up
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < TimeSpan.FromSeconds(5))
            {
                dlqMsg = dlqConsumer.Consume(TimeSpan.FromMilliseconds(500));
                if (dlqMsg?.Message?.Value is not null) break;
            }

            // Assert
            dlqMsg.Should().NotBeNull("a DLQ message should be published when handler keeps failing");
            dlqMsg!.Topic.Should().Be(dlqTopic);
            dlqMsg.Message.Value.Should().Contain("errorType");
        }


        // auto.create.topics.enable in kafka not guaranteed, use this method to explicitly create a topic if needed
        private static async Task EnsureTopicExists(string bootstrap, string topic)
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrap }).Build();
            try
            {
                await admin.CreateTopicsAsync(new[]
                {
                    new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }
                });
            }
            catch (CreateTopicsException ex)
            {
                if (ex.Results.Any(r => r.Error.Code != ErrorCode.TopicAlreadyExists))
                    throw;
            }
        }
    }
}
