using ECommerce.UserService.API.DTOs;
using FluentAssertions;
using System.Net.Http.Json;
using System.Net;
using ECommerce.Tests.Integration.CustomWebAppFactories;

namespace ECommerce.Tests.Integration.ApiTests
{
    public sealed class UserApiTests : IClassFixture<CustomUserServiceWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public UserApiTests(CustomUserServiceWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_Users_Null_Returns_200() // return empty list
        {
            // Arrange
            var isolatedFactory = new CustomUserServiceWebApplicationFactory();
            var isolatedClient = isolatedFactory.CreateClient();

            // Act
            var response = await isolatedClient.GetAsync("/api/v1/users");
            var users = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            users.Should().NotBeNull();
            users.Should().BeEmpty();
        }

        [Fact]
        public async Task Post_User_Returns_201_And_LocationHeader()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/users", new { name = "testSuccess", email = "testSuccess@mail.com" });
            var created = await response.Content.ReadFromJsonAsync<UserResponseDto>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();
            created.Should().NotBeNull();
            created.Id.Should().NotBeEmpty();
            created.Email.Should().Be("testsuccess@mail.com"); // case insensitive
        }

        [Fact]
        public async Task Get_User_ById_Returns_200()
        {
            // Arrange
            var post = await _client.PostAsJsonAsync("/api/v1/users", new { name = "testSuccess2", email = "testSuccess2@mail.com" });
            var created = await post.Content.ReadFromJsonAsync<UserResponseDto>();

            // Act
            var get = await _client.GetAsync($"/api/v1/users/{created!.Id}");

            // Assert
            get.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Post_Users_DuplicateEmail_Returns_400()
        {
            // Act
            await _client.PostAsJsonAsync("/api/v1/users", new { name = "initial", email = "initial@mail.com" });
            var response = await _client.PostAsJsonAsync("/api/v1/users", new { name = "duplicate", email = "initial@mail.com" });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Get_User_Null_Returns_404()
        {
            // Act
            var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Post_Invalid_Users_Returns_400()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/users", new { name = "", email = "not-email" });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
