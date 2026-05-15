using AuctionSystem.API.Data;
using AuctionSystem.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuctionSystem.Tests;

public class AuthServiceTests
{
    private AuctionDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AuctionDbContext(options);
    }

    private IConfiguration GetConfig() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "AuctionSystemSecretKey2024!@#$%^",
            ["Jwt:Issuer"] = "AuctionSystem",
            ["Jwt:Audience"] = "AuctionUsers"
        })
        .Build();

    [Fact]
    public async Task Register_NewUser_ShouldCreateUser()
    {
        using var context = CreateContext(nameof(Register_NewUser_ShouldCreateUser));
        var service = new AuthService(context, GetConfig());

        var user = await service.Register("testuser", "password123", "Bidder");

        Assert.NotNull(user);
        Assert.Equal("testuser", user.Username);
        Assert.Equal("Bidder", user.Role);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnToken()
    {
        using var context = CreateContext(nameof(Login_ValidCredentials_ShouldReturnToken));
        var service = new AuthService(context, GetConfig());
        await service.Register("testuser", "password123", "Bidder");

        var token = await service.Login("testuser", "password123");

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task Login_InvalidPassword_ShouldReturnNull()
    {
        using var context = CreateContext(nameof(Login_InvalidPassword_ShouldReturnNull));
        var service = new AuthService(context, GetConfig());
        await service.Register("testuser", "password123", "Bidder");

        var token = await service.Login("testuser", "wrongpassword");

        Assert.Null(token);
    }
}
