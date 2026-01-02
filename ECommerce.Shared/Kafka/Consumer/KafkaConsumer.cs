using Confluent.Kafka;
using ECommerce.Shared.Kafka.Contracts;
using ECommerce.Shared.Kafka.Interfaces;
using ECommerce.Shared.Kafka.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ECommerce.Shared.Kafka.Consumer
{
    public sealed class KafkaConsumer : BackgroundService
    {
        private readonly ILogger<KafkaConsumer> _logger;
        private readonly KafkaConsumerConfigOptions _options;

        private readonly IServiceScopeFactory _scopeFactory; // hand off message processing by registered services via keys
        private readonly IKafkaProducer _producer; // to send errorneous messages to a DLQ
        private readonly string _dlqTopic;

        public KafkaConsumer(ILogger<KafkaConsumer> logger, IOptions<KafkaConsumerConfigOptions> options, IServiceScopeFactory scopeFactory, IKafkaProducer producer, IConfiguration config)
        {
            _logger = logger;
            _options = options.Value;
            _scopeFactory = scopeFactory;
            _producer = producer;
            _dlqTopic = config["Kafka:Topics:DlqTopic"] ?? throw new InvalidOperationException("Kafka DlqTopic missing");
        }

        protected override Task ExecuteAsync(CancellationToken ct)
        {
            return Task.Run(() => ConsumeLoop(ct), ct);
        }

        private async Task ConsumeLoop(CancellationToken ct)
        {
            var config = new ConsumerConfig { 
                BootstrapServers = _options.BootstrapServers,
                GroupId = _options.GroupId,
                AutoOffsetReset = _options.AutoOffsetReset,
                EnableAutoCommit = _options.EnableAutoCommit,
                MaxPollIntervalMs = _options.MaxPollIntervalMs,
                SessionTimeoutMs = _options.SessionTimeoutMs,
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();

            consumer.Subscribe(_options.Topics);
            _logger.LogInformation("Kafka consumer subscribed to: {Topics}", string.Join(", ", _options.Topics));

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    ConsumeResult<string, string>? cr = null;

                    try
                    {
                        cr = consumer.Consume(ct);
                        if (cr?.Message?.Value is null)
                        {
                            continue;
                        }
                        await ProcessWithRetryAsync(cr, ct);
                        
                        if (!_options.EnableAutoCommit)
                        {
                            consumer.Commit(cr);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Kafka processing error");
                        if (cr is not null)
                        {
                            try
                            {
                                await PublishDlqAsync(cr.Topic, cr.Message.Key, cr.Message.Value, ex, ct);
                                if (!_options.EnableAutoCommit)
                                {
                                    consumer.Commit(cr);
                                }
                            }
                            catch (Exception dlqEx)
                            {
                                // simple error log if even DLQ publishing fails
                                _logger.LogError(dlqEx, "Failed to publish DLQ, offset NOT committed");
                            }
                        }
                    }
                }
            }
            finally
            {
                consumer.Close();
            }
        }

        private async Task ProcessWithRetryAsync(ConsumeResult<string, string> cr, CancellationToken ct)
        {
            // hardcode max attempts
            const int maxAttempts = 3;
            var attempt = 0;
            while (true)
            {
                try
                {
                    await ProcessByTopicAsync(cr.Topic, cr.Message.Key, cr.Message.Value, ct);
                    return;
                }
                catch (Exception) when (++attempt < maxAttempts)
                {
                    await Task.Delay(100, ct);
                }
            }
        }


        private async Task ProcessByTopicAsync(string topic, string key, string raw, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            var handler = sp.GetKeyedService<IKafkaEventHandler>(topic);
            if (handler is null)
            {
                _logger.LogWarning("No handler registered for topic {Topic}. Skipping.", topic);
                return;
            }

            await handler.HandleAsync(key, raw, ct);
        }

        private async Task PublishDlqAsync(string ogTopic, string key, string raw, Exception ex, CancellationToken ct)
        {
            var dlqPayload = new
            {
                ogTopic,
                key,
                raw,
                errorType = ex.GetType().Name,
                errorMessage = ex.Message,
                failedAtUtc = DateTime.UtcNow,
            };

            var dlqEvent = new KafkaEvent<object>(
                EventId: Guid.NewGuid(),
                EventType: _dlqTopic,
                Version: 1,
                OccurredAtUtc: DateTime.UtcNow,
                Producer: "none", // change to service name
                Key: key ?? "dlq",
                Data: dlqPayload
            );

            await _producer.PublishAsync(_dlqTopic, dlqEvent.Key, dlqEvent, ct);
        }
    }
}
