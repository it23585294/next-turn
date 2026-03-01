using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Infrastructure.Auth;
using NextTurn.Infrastructure.BusinessRegistry;
using NextTurn.Infrastructure.Email;
using NextTurn.Infrastructure.Persistence;

namespace NextTurn.Infrastructure;

/// <summary>
/// Extension method to register all Infrastructure services into the DI container.
/// Called once from Program.cs: builder.Services.AddInfrastructure(builder.Configuration)
///
/// This keeps Program.cs clean and gives Infrastructure full control over
/// how its own services are registered.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ─────────────────────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlServerOptions =>
                {
                    // Retry on transient failures (e.g. brief Azure SQL connectivity blips)
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

        // Register ApplicationDbContext as IApplicationDbContext so Application
        // layer handlers can depend on the interface, not the concrete class
        services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());

        // ── Multi-tenancy ─────────────────────────────────────────────────────
        // IHttpContextAccessor makes the current HttpContext available in DI services.
        // Required by HttpTenantContext to read HttpContext.Items populated by TenantMiddleware.
        services.AddHttpContextAccessor();
        // Scoped: one HttpTenantContext per HTTP request, same lifetime as DbContext.
        services.AddScoped<ITenantContext, HttpTenantContext>();

        // ── Repositories ──────────────────────────────────────────────────────
        // Scoped lifetime matches DbContext — one instance per HTTP request.
        services.AddScoped<IUserRepository, UserRepository>();

        // ── Security ──────────────────────────────────────────────────────────
        // Singleton is safe — BcryptPasswordHasher holds no state.
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        // ── JWT ───────────────────────────────────────────────────────────────
        // Bind the "JwtSettings" section to the strongly-typed POCO.
        // IOptions<JwtSettings> is then injected into JwtTokenService.
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        // Scoped matches the request lifetime — consistent with other auth services.
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // ── External service stubs (Sprint 1) ─────────────────────────────────
        // Real implementations (SMTP/SendGrid, business registry API) are wired
        // in a later sprint — swap these registrations then.
        services.AddScoped<IEmailService, StubEmailService>();
        services.AddScoped<IBusinessRegistryService, StubBusinessRegistryService>();

        return services;
    }
}
