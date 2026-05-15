using System.Text;
using System.Security.Claims;
using AuctionSystem.API.Data;
using AuctionSystem.API.Hubs;
using AuctionSystem.API.Middleware;
using AuctionSystem.API.Realtime;
using AuctionSystem.Application.Abstractions.Realtime;
using AuctionSystem.Application.Behaviors;
using AuctionSystem.Application.Authentication.Login;
using FluentValidation;
using AuctionSystem.Infrastructure;
using AuctionSystem.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
        });
        builder.Services.AddControllers();
        builder.Services.AddDataProtection();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("ClientApp", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:4200",
                        "https://localhost:4200",
                        "http://127.0.0.1:4200",
                        "https://127.0.0.1:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        builder.Services.AddSignalR();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddMediatR(typeof(LoginCommand).Assembly);
        builder.Services.AddValidatorsFromAssembly(typeof(LoginCommand).Assembly);
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        builder.Services.AddScoped<IAuctionRealtimeNotifier, SignalRAuctionRealtimeNotifier>();
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    NameClaimType = JwtRegisteredClaimNames.Email,
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.Zero
                };
            });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Admin") ||
                    context.User.HasClaim("role", "Admin") ||
                    context.User.HasClaim(ClaimTypes.Role, "Admin"));
            });
        });

        var app = builder.Build();
        DatabaseSeeder.InitializeAsync(app.Services, app.Configuration, app.Environment, app.Logger).GetAwaiter().GetResult();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseMiddleware<ApiExceptionMiddleware>();
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();
        app.UseCors("ClientApp");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<AuctionHub>("/hubs/auctions");

        app.Run();
    }
}
