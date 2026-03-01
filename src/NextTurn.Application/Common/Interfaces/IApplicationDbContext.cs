using Microsoft.EntityFrameworkCore;
using NextTurn.Domain.Auth.Entities;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.Application.Common.Interfaces;

/// <summary>
/// Abstraction over ApplicationDbContext exposed to the Application layer.
/// Application handlers depend on this interface, never on the concrete DbContext.
/// This keeps the Application layer free of direct EF Core coupling.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<OrganisationEntity> Organisations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
