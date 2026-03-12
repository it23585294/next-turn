namespace NextTurn.Application.Common.Interfaces;

/// <summary>
/// Contract for validating an organisation against an external business registry.
/// In Sprint 1, a stub that always returns <c>true</c> is registered, representing
/// the interface contract for future third-party API integration.
/// </summary>
public interface IBusinessRegistryService
{
    /// <summary>
    /// Returns <c>true</c> if the given organisation name is recognised as a
    /// legitimate registered business in the specified country; otherwise <c>false</c>.
    /// </summary>
    Task<bool> IsRegisteredBusinessAsync(
        string orgName,
        string country,
        CancellationToken cancellationToken);
}
