using ECommerce.OrderService.API.DTOs;
using ECommerce.Tests.Integration.CustomWebAppFactories;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Tests.Integration
{
    public sealed class OrderApiTests : IClassFixture<CustomOrderServiceWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public OrderApiTests(CustomOrderServiceWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Post_Order_Returns_201_And_LocationHeader()
        {
            // Act
            var payload = new OrderRequestDto(Guid.NewGuid(), "testProduct", 1, 10.50m);
            var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);
            var created = await response.Content.ReadFromJsonAsync<OrderResponseDto>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();
            created.Should().NotBeNull();
            created!.Id.Should().NotBeEmpty();
            created.Product.Should().Be("testProduct");
        }

        [Fact]
        public async Task Get_Order_ById_Returns_200()
        {
            // Arrange
            var payload = new OrderRequestDto(Guid.NewGuid(), "testProduct2", 2, 11.00m);
            var post = await _client.PostAsJsonAsync("/api/v1/orders", payload);
            var created = await post.Content.ReadFromJsonAsync<OrderResponseDto>();

            // Act
            var get = await _client.GetAsync($"/api/v1/orders/{created!.Id}");

            // Assert
            get.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Get_Order_Null_Returns_404()
        {
            // Act
            var response = await _client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Get_Orders_ByUser_Returns_200_And_List()
        {
            // Arrange
            var userId = Guid.NewGuid();

            await _client.PostAsJsonAsync("/api/v1/orders", new OrderRequestDto(userId, "testProduct1", 5, 10.50m));
            await _client.PostAsJsonAsync("/api/v1/orders", new OrderRequestDto(userId, "testProduct2", 10, 11.00m));

            // Act
            var response = await _client.GetAsync($"/api/v1/orders/user/{userId}");
            var orders = await response.Content.ReadFromJsonAsync<List<OrderResponseDto>>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            orders.Should().NotBeNull();
            orders.Count.Should().Be(2);
        }

        [Fact]
        public async Task Post_Invalid_Order_Returns_400()
        {
            // Arrange
            var payload = new OrderRequestDto(Guid.NewGuid(), "", 0, -1m);

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/orders", payload);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
