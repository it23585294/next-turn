using Microsoft.EntityFrameworkCore;
using NextTurn.Domain.Organisation.Repositories;
using NextTurn.Infrastructure.Persistence;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.Infrastructure.Organisation;

/// <summary>
/// EF Core implementation of IOrganisationRepository.
///
/// Important: AddAsync deliberately does NOT call SaveChangesAsync.
/// The RegisterOrganisationCommandHandler saves both the Organisation and
/// the OrgAdmin User in a single SaveChangesAsync call for atomicity.
/// </summary>
public sealed class OrganisationRepository : IOrganisationRepository
{
    private readonly ApplicationDbContext _context;

    public OrganisationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<OrganisationEntity?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _context.Organisations
            .FirstOrDefaultAsync(o => o.Name == name, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(
        OrganisationEntity organisation,
        CancellationToken cancellationToken = default)
    {
        // Stage the entity for insertion. The caller is responsible for
        // calling SaveChangesAsync — this keeps the insert atomic with any
        // related entities (e.g. the OrgAdmin User) added in the same handler.
        await _context.Organisations.AddAsync(organisation, cancellationToken);
    }
}
