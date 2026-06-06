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
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

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
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedHost |
                ForwardedHeaders.XForwardedProto;
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("ForgotPassword", limiterOptions =>
            {
                limiterOptions.PermitLimit = 5;
                limiterOptions.Window = TimeSpan.FromMinutes(15);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
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
        app.UseForwardedHeaders();
        app.Use((context, next) =>
        {
            if (TryGetForwardedPrefix(context.Request, out var pathBase))
            {
                context.Request.PathBase = pathBase;
            }

            return next();
        });

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();
        app.UseCors("ClientApp");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<AuctionHub>("/hubs/auctions");

        app.Run();
    }

    private static bool TryGetForwardedPrefix(HttpRequest request, out PathString pathBase)
    {
        pathBase = PathString.Empty;

        if (!request.Headers.TryGetValue("X-Forwarded-Prefix", out var values))
        {
            return false;
        }

        var prefix = values.ToString().Split(',', 2)[0].Trim().TrimEnd('/');

        if (string.IsNullOrWhiteSpace(prefix) ||
            !prefix.StartsWith("/", StringComparison.Ordinal) ||
            prefix.Contains("://", StringComparison.Ordinal) ||
            prefix.Contains('?') ||
            prefix.Contains('#'))
        {
            return false;
        }

        pathBase = new PathString(prefix);
        return true;
    }
}
