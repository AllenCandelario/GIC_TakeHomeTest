namespace ECommerce.UserService.API.DTOs
{
    public sealed record UserRequestDto(string Name, string Email);

    public sealed record UserResponseDto(Guid Id, string Name, string Email, DateTime CreatedAtUtc);
}
