using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Application.Common.Interfaces;
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

        return services;
    }
}
