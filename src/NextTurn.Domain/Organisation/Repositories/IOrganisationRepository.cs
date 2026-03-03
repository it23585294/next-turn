namespace NextTurn.Domain.Organisation.Repositories;

using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

/// <summary>
/// Persistence contract for the Organisation aggregate root.
/// Implemented in NextTurn.Infrastructure; consumed by Application command handlers.
/// </summary>
public interface IOrganisationRepository
{
    /// <summary>
    /// Returns the organisation with the given name, or null if none exists.
    /// Used to enforce the unique-name constraint at the domain boundary before
    /// attempting to persist (avoids relying solely on DB unique index for domain logic).
    /// </summary>
    Task<OrganisationEntity?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a newly created organisation.
    /// The caller is responsible for committing the unit of work.
    /// </summary>
    Task AddAsync(OrganisationEntity organisation, CancellationToken cancellationToken);
}
