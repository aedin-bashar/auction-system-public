using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace AuctionSystem.IntegrationTests.Admin.ReportsAndSettings;

public sealed class AdminReportsAndSettingsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"AuctionSystem-AdminReportsAndSettingsTests-{Guid.NewGuid()}";

    public Mock<IPasswordVerifier> PasswordVerifierMock { get; } = new();
    public Mock<IPasswordStore> PasswordStoreMock { get; } = new();
    public Mock<ITokenService> TokenServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            var sqlServerDescriptors = services
                .Where(d => (d.ServiceType.FullName?.Contains("SqlServer") ?? false) ||
                            (d.ImplementationType?.FullName?.Contains("SqlServer") ?? false))
                .ToList();

            foreach (var descriptor in sqlServerDescriptors)
            {
                services.Remove(descriptor);
            }

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
                    .UseInternalServiceProvider(inMemoryProvider));

            services.RemoveAll<IUserRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.RemoveAll<IPasswordVerifier>();
            services.RemoveAll<IPasswordStore>();
            services.RemoveAll<ITokenService>();

            services.AddSingleton(PasswordVerifierMock.Object);
            services.AddSingleton(PasswordStoreMock.Object);
            services.AddSingleton(TokenServiceMock.Object);
        });
    }

    public async Task<Guid> SeedUserAsync(string email, string fullName, UserRole role, bool isActive = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = User.Register(email, fullName, role);
        if (!isActive)
        {
            user.Deactivate();
        }

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }
}
