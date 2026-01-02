namespace ECommerce.OrderService.API.DTOs
{
    public sealed record OrderRequestDto(Guid UserId, string Product, int Quantity, decimal Price);

    public sealed record OrderResponseDto(Guid Id, Guid UserId, string Product, int Quantity, decimal Price, DateTime CreatedAtUtc);
}