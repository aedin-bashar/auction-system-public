using AuctionSystem.Application.Admin.Moderation;
using AuctionSystem.Application.Admin.Reports;
using AuctionSystem.Application.Admin.SystemSettings;
using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Application.Payments.Admin.TransactionManagement;
using AuctionSystem.Application.Users.PaymentMethods;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Infrastructure;
using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.Infrastructure.Repositories;
using AuctionSystem.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuctionSystem.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_WhenServicesNull_ThrowsArgumentNullException()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => DependencyInjection.AddInfrastructure(null!, configuration));
    }

    [Fact]
    public void AddInfrastructure_WhenConfigurationNull_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddInfrastructure(null!));
    }

    [Fact]
    public void AddInfrastructure_WhenConnectionStringMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(configuration));

        Assert.Contains("DefaultConnection", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddInfrastructure_WithValidConfiguration_RegistersInfrastructureServicesAndBindsJwtOptions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=AuctionSystem.Tests;Trusted_Connection=True;TrustServerCertificate=True",
                ["Jwt:Issuer"] = "AuctionSystem.Tests",
                ["Jwt:Audience"] = "AuctionSystem.Tests.Client",
                ["Jwt:SigningKey"] = "ThisIsATestSigningKeyWithAtLeast32Chars!",
                ["Jwt:AccessTokenMinutes"] = "90"
            })
            .Build();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var jwtOptions = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
        var dbContext = provider.GetRequiredService<ApplicationDbContext>();

        Assert.NotNull(dbContext);
        Assert.Equal("AuctionSystem.Tests", jwtOptions.Issuer);
        Assert.Equal("AuctionSystem.Tests.Client", jwtOptions.Audience);
        Assert.Equal("ThisIsATestSigningKeyWithAtLeast32Chars!", jwtOptions.SigningKey);
        Assert.Equal(90, jwtOptions.AccessTokenMinutes);

        Assert.Contains(services, x => x.ServiceType == typeof(IUnitOfWork) && x.ImplementationType == typeof(UnitOfWork));
        Assert.Contains(services, x => x.ServiceType == typeof(IUserRepository) && x.ImplementationType == typeof(UserRepository));
        Assert.Contains(services, x => x.ServiceType == typeof(IAuctionRepository) && x.ImplementationType == typeof(AuctionRepository));
        Assert.Contains(services, x => x.ServiceType == typeof(IAuctionReportStore) && x.ImplementationType == typeof(AuctionReportStore));
        Assert.Contains(services, x => x.ServiceType == typeof(IPaymentMethodStore) && x.ImplementationType == typeof(PaymentMethodStore));
        Assert.Contains(services, x => x.ServiceType == typeof(IAdminTransactionStore) && x.ImplementationType == typeof(AdminTransactionStore));
        Assert.Contains(services, x => x.ServiceType == typeof(IAdminReportStore) && x.ImplementationType == typeof(AdminReportStore));
        Assert.Contains(services, x => x.ServiceType == typeof(IAdminSystemSettingsStore) && x.ImplementationType == typeof(AdminSystemSettingsStore));
        Assert.Contains(services, x => x.ServiceType == typeof(IPasswordStore) && x.ImplementationType == typeof(PasswordStore));
        Assert.Contains(services, x => x.ServiceType == typeof(IPasswordVerifier) && x.ImplementationType == typeof(PasswordVerifier));
        Assert.Contains(services, x => x.ServiceType == typeof(ITokenService) && x.ImplementationType == typeof(TokenService));
    }
}