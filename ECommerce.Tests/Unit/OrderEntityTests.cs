using ECommerce.OrderService.Domain;
using FluentAssertions;

namespace ECommerce.Tests.Unit
{
    public sealed class OrderEntityTests
    {
        [Theory]
        [InlineData("testProduct", 1, 10.50, "testProduct", 1, 10.50)]
        [InlineData(" whitespaceProduct ", 2, 11, "whitespaceProduct", 2, 11)]
        public void CreateOrder_ValidData_Success(string productInput, int quantityInput, decimal priceInput, string productOutput, int quantityOutput, decimal priceOutput)
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var order = new Order(userId, productInput, quantityInput, priceInput);

            // Assert
            order.Id.Should().NotBeEmpty();
            order.UserId.Should().Be(userId);
            order.Product.Should().Be(productOutput);
            order.Quantity.Should().Be(quantityOutput);
            order.Price.Should().Be(priceOutput);
            order.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(3));
        }

        [Fact]
        public void CreateOrder_EmptyUserId_ThrowsArgumentException()
        {
            // Act
            Action act = () => new Order(Guid.Empty, "testProduct", 1, 10.50m);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("userId");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void CreateOrder_InvalidProduct_ThrowsArgumentException(string product)
        {
            // Act
            Action act = () => new Order(Guid.NewGuid(), product, 1, 1.00m);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("product");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CreateOrder_InvalidQuantity_ThrowsArgumentException(int quantity)
        {
            // Act
            Action act = () => new Order(Guid.NewGuid(), "Product", quantity, 1.00m);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("quantity");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1.234)]
        public void CreateOrder_InvalidPrice_ThrowsArgumentException(decimal price)
        {
            // Act
            Action act = () => new Order(Guid.NewGuid(), "Product", 1, price);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("price");
        }
    }
}