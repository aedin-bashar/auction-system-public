using System;
using AuctionSystem.Application.Admin.Moderation;
using AuctionSystem.Application.Admin.Reports;
using AuctionSystem.Application.Admin.SystemSettings;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Application.Payments.Admin.TransactionManagement;
using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Application.Users.PaymentMethods;
using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.Infrastructure.Repositories;
using AuctionSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found. " +
                "Ensure it exists under ConnectionStrings in appsettings.json.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(120);
            }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.Configure<JwtOptions>(options =>
        {
            var section = configuration.GetSection("Jwt");
            var issuer = section["Issuer"];
            if (!string.IsNullOrWhiteSpace(issuer))
            {
                options.Issuer = issuer;
            }

            var audience = section["Audience"];
            if (!string.IsNullOrWhiteSpace(audience))
            {
                options.Audience = audience;
            }

            var signingKey = section["SigningKey"];
            if (!string.IsNullOrWhiteSpace(signingKey))
            {
                options.SigningKey = signingKey;
            }

            if (int.TryParse(section["AccessTokenMinutes"], out var minutes) && minutes > 0)
            {
                options.AccessTokenMinutes = minutes;
            }
        });

        // Generic repository (optional but useful for app-layer handlers that depend on IRepository<,>)
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));

        // Concrete repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuctionRepository, AuctionRepository>();
        services.AddScoped<IAuctionReportStore, AuctionReportStore>();
        services.AddScoped<IPaymentMethodStore, PaymentMethodStore>();
        services.AddScoped<IAdminTransactionStore, AdminTransactionStore>();
        services.AddScoped<IAdminReportStore, AdminReportStore>();
        services.AddScoped<IAdminSystemSettingsStore, AdminSystemSettingsStore>();

        services.AddScoped<IPasswordStore, PasswordStore>();
        services.AddScoped<IPasswordVerifier, PasswordVerifier>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
