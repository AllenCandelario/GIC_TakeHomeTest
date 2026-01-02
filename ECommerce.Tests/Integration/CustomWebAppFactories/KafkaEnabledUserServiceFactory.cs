using ECommerce.Shared.Kafka.Interfaces;
using ECommerce.Shared.Kafka.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ECommerce.Tests.Integration.CustomWebAppFactories
{
    public sealed class KafkaEnabledUserServiceFactory : WebApplicationFactory<UserService.Program>
    {
        private readonly string _bootstrap;
        public RecordingKafkaHandler RecordingHandler { get; } = new();
        public bool UseThrowingHandler { get; set; }

        public KafkaEnabledUserServiceFactory(string bootstrap)
        {
            _bootstrap = bootstrap;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Kafka:Consumer:BootstrapServers"] = _bootstrap,
                    ["Kafka:Producer:BootstrapServers"] = _bootstrap,
                    ["Kafka:Topics:DlqTopic"] = "user-service.dlq.v1",
                });
            });

            builder.ConfigureServices(services =>
            {

                // remove existing IKafkaEventHandler registrations
                services.RemoveAll<IKafkaEventHandler>();

                // register either recording handler or throwing handler for the topic
                if (UseThrowingHandler)
                {
                    services.AddKeyedSingleton<IKafkaEventHandler>("order.created.v1", new ThrowingKafkaHandler());
                }
                else
                {
                    services.AddKeyedSingleton<IKafkaEventHandler>("order.created.v1", RecordingHandler);
                }
            });
        }
    }
}
