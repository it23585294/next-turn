using FluentValidation;
using MediatR;

namespace NextTurn.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs FluentValidation validators for every
/// command or query before the handler executes.
///
/// How it works:
///   MediatR resolves all IValidator&lt;TRequest&gt; registered in DI.
///   If any validation fails, a ValidationException is thrown before the
///   handler is ever called — the handler only runs when input is valid.
///
/// Why a pipeline behaviour over in-handler validation?
///   - Handlers stay focused on business logic, not input sanitation.
///   - Validation is automatic for every command/query — impossible to forget.
///   - The ValidationExceptionMiddleware in the API layer catches the exception
///     and maps it to a well-formed 422 Unprocessable Entity response.
///
/// Registration:
///   AddApplication() in Application/DependencyInjection.cs wires this up with:
///     services.AddTransient(typeof(IPipelineBehavior&lt;,&gt;), typeof(ValidationBehavior&lt;,&gt;))
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // No validators registered for this request — skip directly to handler.
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        // Run all validators and collect every failure across all rules.
        // ValidateAsync is used so async validators (e.g. DB uniqueness checks
        // at the application-layer level) are supported in future.
        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}
