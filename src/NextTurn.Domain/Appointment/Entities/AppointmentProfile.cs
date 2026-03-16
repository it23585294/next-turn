using NextTurn.Domain.Common;

namespace NextTurn.Domain.Appointment.Entities;

/// <summary>
/// Configurable appointment stream under an organisation, similar to queue definitions.
/// Each profile has independent booking settings and shareable link.
/// </summary>
public sealed class AppointmentProfile
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public string ShareableLink { get; private set; }

    private AppointmentProfile()
    {
        Name = string.Empty;
        ShareableLink = string.Empty;
    }

    private AppointmentProfile(
        Guid id,
        Guid organisationId,
        string name,
        bool isActive,
        string shareableLink)
    {
        Id = id;
        OrganisationId = organisationId;
        Name = name;
        IsActive = isActive;
        ShareableLink = shareableLink;
    }

    public static AppointmentProfile Create(Guid organisationId, string name)
    {
        if (organisationId == Guid.Empty)
            throw new DomainException("Organisation ID is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Appointment profile name is required.");

        var id = Guid.NewGuid();
        var trimmedName = name.Trim();
        var link = $"/appointments/{organisationId}/{id}";

        return new AppointmentProfile(
            id,
            organisationId,
            trimmedName,
            true,
            link);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Appointment profile name is required.");

        Name = name.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
