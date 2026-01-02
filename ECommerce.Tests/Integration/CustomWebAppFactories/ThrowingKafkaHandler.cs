using ECommerce.Shared.Kafka.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Tests.Integration.CustomWebAppFactories
{
    public sealed class ThrowingKafkaHandler : IKafkaEventHandler
    {
        public Task HandleAsync(string key, string rawJson, CancellationToken ct)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
