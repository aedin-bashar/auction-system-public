using AuctionSystem.Application.Authentication.Register;

namespace AuctionSystem.UnitTests.Application.Authentication;

public class RegisterCommandValidatorTests
{
    [Fact]
    public void Validate_WithValidData_Passes()
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("john@example.com", "Secret123!", "John Doe", "+1 (555) 123-4567");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Secret123!", "John Doe")]
    [InlineData("not-an-email", "Secret123!", "John Doe")]
    [InlineData("john@example.com", "", "John Doe")]
    [InlineData("john@example.com", "Secret123!", "A")]
    public void Validate_WithInvalidData_Fails(string email, string password, string fullName)
    {
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand(email, password, fullName, null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
