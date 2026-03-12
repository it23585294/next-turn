using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Application.Auth.Commands.RegisterUser;
using NextTurn.Application.Common.Behaviours;

namespace NextTurn.Application;

/// <summary>
/// Extension method to register all Application layer services into the DI container.
/// Called from Program.cs: builder.Services.AddApplication()
///
/// Registers:
///   - MediatR (scans this assembly for IRequestHandler, INotificationHandler)
///   - FluentValidation validators (scans this assembly for AbstractValidator&lt;T&gt;)
///   - ValidationBehavior pipeline (runs validators before every handler)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(RegisterUserCommand).Assembly;

        // ── MediatR ───────────────────────────────────────────────────────────
        // Registers all IRequestHandler<,> and INotificationHandler<> in this assembly.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(assembly));

        // ── FluentValidation ──────────────────────────────────────────────────
        // Scans the assembly and registers every AbstractValidator<T> in DI so
        // ValidationBehavior can resolve IEnumerable<IValidator<TRequest>>.
        services.AddValidatorsFromAssembly(assembly);

        // ── Pipeline behaviours ───────────────────────────────────────────────
        // Open-generic registration: for every TRequest/TResponse pair, MediatR
        // will resolve this behaviour and run it before the handler.
        // Transient matches MediatR's default handler lifetime.
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        return services;
    }
}
