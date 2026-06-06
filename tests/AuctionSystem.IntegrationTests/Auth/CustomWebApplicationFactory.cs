using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Application.Abstractions.Email;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace AuctionSystem.IntegrationTests.Auth;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"AuctionSystem-TestDb-{Guid.NewGuid()}";

    public Mock<IPasswordVerifier> PasswordVerifierMock { get; } = new();
    public Mock<IPasswordStore> PasswordStoreMock { get; } = new();
    public Mock<ITokenService> TokenServiceMock { get; } = new();
    public Mock<IEmailSender> EmailSenderMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = string.Empty
            });
        });

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
            services.RemoveAll<IEmailSender>();

            services.AddSingleton(PasswordVerifierMock.Object);
            services.AddSingleton(PasswordStoreMock.Object);
            services.AddSingleton(TokenServiceMock.Object);
            services.AddSingleton(EmailSenderMock.Object);
        });
    }
}
