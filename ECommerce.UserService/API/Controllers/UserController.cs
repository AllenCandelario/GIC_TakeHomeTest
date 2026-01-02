using ECommerce.UserService.API.DTOs;
using ECommerce.UserService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.UserService.API.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    public sealed class UserController : ControllerBase
    {
        private readonly IUserQueryHandler _queryHandler;
        
        public UserController(IUserQueryHandler queryHandler)
        {
            _queryHandler = queryHandler;
        }

        [HttpPost("")]
        public async Task<ActionResult<UserResponseDto>> AddUserAsync([FromBody] UserRequestDto userDto, CancellationToken ct)
        {
            var user = await _queryHandler.AddUserAsync(userDto.Name, userDto.Email, ct);
            var response = new UserResponseDto(user.Id, user.Name, user.Email, user.CreatedAtUtc);
            return Created($"/api/v1/users/{user.Id}", response);
        }

        [HttpGet("")]
        public async Task<ActionResult<List<UserResponseDto>>> GetAllUsersAsync(CancellationToken ct)
        {
            var users = await _queryHandler.GetAllUsersAsync(ct);
            var response = users.Select(u => new UserResponseDto(u.Id, u.Name, u.Email, u.CreatedAtUtc)).ToList();
            return Ok(response);
        }

        [HttpGet("{userId:guid}")]
        public async Task<ActionResult<UserResponseDto>> GetUserByIdAsync(Guid userId, CancellationToken ct)
        {
            var user = await _queryHandler.GetUserByIdAsync(userId, ct);
            var response = new UserResponseDto(user.Id, user.Name, user.Email, user.CreatedAtUtc);
            return Ok(response);
        }
    }
}
