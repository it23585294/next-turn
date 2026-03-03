using Microsoft.Extensions.Logging;
using NextTurn.Application.Common.Interfaces;

namespace NextTurn.Infrastructure.BusinessRegistry;

/// <summary>
/// No-op implementation of <see cref="IBusinessRegistryService"/> used in Sprint 1.
/// Always returns <c>true</c>, representing the pass-through contract for future
/// third-party business registry API integration.
/// </summary>
public sealed class StubBusinessRegistryService : IBusinessRegistryService
{
    private readonly ILogger<StubBusinessRegistryService> _logger;

    public StubBusinessRegistryService(ILogger<StubBusinessRegistryService> logger)
    {
        _logger = logger;
    }

    public Task<bool> IsRegisteredBusinessAsync(
        string orgName,
        string country,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[STUB] Business registry check skipped for '{OrgName}' in {Country}. " +
            "Returning true (stub always passes).",
            orgName,
            country);

        return Task.FromResult(true);
    }
}
