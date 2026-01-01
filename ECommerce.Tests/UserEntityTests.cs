using ECommerce.UserService.Domain;
using FluentAssertions;

namespace ECommerce.Tests
{
    public sealed class UserEntityTests
    {
        [Theory]
        [InlineData("test", "test@mail.com", "test", "test@mail.com")]
        [InlineData(" whitespace ", "  whitespace@mail.com  ", "whitespace", "whitespace@mail.com")]
        public void CreateUser_ValidData_Success(string nameInput, string emailInput, string expectedName, string expectedEmail)
        {
            // Act
            var user = new User(nameInput, emailInput);

            // Assert
            user.Id.Should().NotBeEmpty();
            user.Name.Should().Be(expectedName);
            user.Email.Should().Be(expectedEmail);
            user.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData("", "emptyName@mail.com")]
        [InlineData("  ", "whitespaceOnlyName@mail.com")]
        [InlineData(null, "nullName@mail.com")]
        public void CreateUser_InvalidName_ThrowsArgumentException(string name, string email)
        {
            // Act
            Action act = () => new User(name, email);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("name");
        }

        [Theory]
        [InlineData("emptyMail", "")]
        [InlineData("whitespaceMail", "  ")]
        [InlineData("nullMail", null)]
        [InlineData("missing@Mail", "missing.com")]
        [InlineData("starting@Mail", "@starting.com")]
        [InlineData("trailing@Mail", "trailing.com@")]
        [InlineData("multiple@Mail", "multiple@mail@hotmail@gmail.com")]
        public void CreateUser_InvalidEmail_ThrowsArgumentException(string name, string email)
        {
            // Act
            Action act = () => new User(name, email);

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("email");
        }
    }
}
