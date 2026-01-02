using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Shared.Kafka.Options
{
    public sealed class KafkaProducerConfigOptions
    {
        public required string BootstrapServers { get; init; }
        public required string ClientId { get; init; }
    }
}
