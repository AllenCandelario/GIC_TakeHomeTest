using ECommerce.Shared.Kafka.Contracts;
using ECommerce.Shared.Kafka.Interfaces;
using ECommerce.UserService.Application.Interfaces;
using ECommerce.UserService.Application.Services;
using ECommerce.UserService.Domain;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Unit
{
    public sealed class KafkaUnitTests
    {
        [Fact]
        public async Task OrderCreatedHandler_InvalidJson_DoesNotThrow()
        {
            // Arrange
            var logger = new Mock<ILogger<OrderCreatedEventHandler>>();
            var handler = new OrderCreatedEventHandler(logger.Object);

            // Act
            Func<Task> act = () => handler.HandleAsync("k", "not-json", CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task AddUserAsync_Publishes_UserCreated_Event()
        {
            // Arrange
            var repo = new Mock<IUserRepository>(MockBehavior.Strict);
            var producer = new Mock<IKafkaProducer>(MockBehavior.Strict);

            // no duplicate
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
            
            repo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:Topics:UserCreatedTopic"] = "user.created.v1"
            }).Build();

            // producer publish should succeed
            producer.Setup(p => p.PublishAsync(
                "user.created.v1",
                It.IsAny<string>(),
                It.IsAny<KafkaEvent<UserCreatedEventV1>>(),
                It.IsAny<CancellationToken>())
            ).Returns(Task.CompletedTask);

            var sut = new UserService.Application.Services.UserService(repo.Object, producer.Object, config);

            // Act
            var user = await sut.AddUserAsync("test", "test@mail.com", CancellationToken.None);

            // Assert
            producer.Verify(p => p.PublishAsync(
                "user.created.v1",
                user.Id.ToString(),
                It.Is<KafkaEvent<UserCreatedEventV1>>(e =>
                   e.EventType == "user.created.v1" &&
                   e.Data.UserId == user.Id &&
                   e.Data.Email == "test@mail.com"
                ),
                It.IsAny<CancellationToken>()), Times.Once
            );
        }
    }
}
