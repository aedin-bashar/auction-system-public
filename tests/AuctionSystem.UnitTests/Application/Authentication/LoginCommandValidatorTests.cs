using AuctionSystem.Application.Authentication.Login;

namespace AuctionSystem.UnitTests.Application.Authentication;

public class LoginCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidData_Passes()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("john@example.com", "Secret123!");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Secret123!")]
    [InlineData("not-an-email", "Secret123!")]
    [InlineData("john@example.com", "")]
    public void Validate_WithInvalidData_Fails(string email, string password)
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand(email, password);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
