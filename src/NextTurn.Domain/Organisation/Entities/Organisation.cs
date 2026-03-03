using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;

namespace NextTurn.Domain.Organisation.Entities;

/// <summary>
/// Aggregate root representing an organisation registered on the NextTurn platform.
///
/// Invariants enforced by the constructor / factory method:
///   - Name is non-empty and ≤ 200 characters
///   - AdminEmail is a valid email address (delegated to the EmailAddress value object)
///   - Type is a known OrganisationType value
///   - Address is fully specified (delegated to the Address value object)
///
/// Status transitions (Approve / Suspend / Reinstate) are intentionally NOT
/// implemented in Sprint 1 — stub methods are left as placeholders so the
/// persistence schema is stable for Sprint 2.
/// </summary>
public class Organisation
{
    // ── Identity ─────────────────────────────────────────────────────────────
    public Guid                Id          { get; }
    public string              Name        { get; private set; }
    public Address             Address     { get; private set; }
    public OrganisationType    Type        { get; private set; }
    public OrganisationStatus  Status      { get; private set; }
    public EmailAddress        AdminEmail  { get; private set; }
    public DateTimeOffset      CreatedAt   { get; }

    // Required by EF Core for entity materialisation — prevents direct construction.
    protected Organisation()
    {
        // default! suppresses CS8618 — EF Core assigns these before the instance is ever read.
        Name       = default!;
        Address    = default!;
        AdminEmail = default!;
    }

    private Organisation(
        Guid               id,
        string             name,
        Address            address,
        OrganisationType   type,
        OrganisationStatus status,
        EmailAddress       adminEmail,
        DateTimeOffset     createdAt)
    {
        Id         = id;
        Name       = name;
        Address    = address;
        Type       = type;
        Status     = status;
        AdminEmail = adminEmail;
        CreatedAt  = createdAt;
    }

    /// <summary>
    /// Creates a new organisation in <see cref="OrganisationStatus.PendingApproval"/> status.
    /// </summary>
    public static Organisation Create(
        string           name,
        Address          address,
        OrganisationType type,
        EmailAddress     adminEmail)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Organisation name is required.");

        if (name.Length > 200)
            throw new DomainException("Organisation name must not exceed 200 characters.");

        return new Organisation(
            id:         Guid.NewGuid(),
            name:       name.Trim(),
            address:    address,
            type:       type,
            status:     OrganisationStatus.PendingApproval,
            adminEmail: adminEmail,
            createdAt:  DateTimeOffset.UtcNow);
    }

    // ── Status transitions (Sprint 2) ─────────────────────────────────────

    /// <summary>Moves the organisation to Active. Called by a SystemAdmin. (Sprint 2)</summary>
    public void Approve()
    {
        if (Status != OrganisationStatus.PendingApproval)
            throw new DomainException("Only a pending organisation can be approved.");

        Status = OrganisationStatus.Active;
    }

    /// <summary>Suspends an active organisation. Called by a SystemAdmin. (Sprint 2)</summary>
    public void Suspend()
    {
        if (Status != OrganisationStatus.Active)
            throw new DomainException("Only an active organisation can be suspended.");

        Status = OrganisationStatus.Suspended;
    }

    /// <summary>Reinstates a suspended organisation. Called by a SystemAdmin. (Sprint 2)</summary>
    public void Reinstate()
    {
        if (Status != OrganisationStatus.Suspended)
            throw new DomainException("Only a suspended organisation can be reinstated.");

        Status = OrganisationStatus.Active;
    }
}
