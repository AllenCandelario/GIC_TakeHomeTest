namespace ECommerce.Shared.Kafka.Contracts
{
    public sealed record UserCreatedEventV1(Guid UserId, string Name, string Email, DateTime CreatedAtUtc);
}
