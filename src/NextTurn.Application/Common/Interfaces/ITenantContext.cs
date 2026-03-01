namespace NextTurn.Application.Common.Interfaces;

/// <summary>
/// Exposes the current request's tenant identity.
/// Implemented in Infrastructure by reading the TenantId JWT claim.
/// Registered as scoped — one instance per HTTP request.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
}
