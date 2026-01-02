using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Shared.Kafka.Contracts
{
    public sealed record OrderCreatedEventV1(Guid OrderId, Guid UserId, string Product, int Quantity, decimal Price, DateTime CreatedAtUtc);
}
