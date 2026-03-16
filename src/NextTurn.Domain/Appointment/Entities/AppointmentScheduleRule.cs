using NextTurn.Domain.Common;

namespace NextTurn.Domain.Appointment.Entities;

/// <summary>
/// Configures how an organisation offers appointment slots for a specific day of week.
/// One row per organisation + day (0=Sunday ... 6=Saturday).
/// </summary>
public sealed class AppointmentScheduleRule
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public Guid AppointmentProfileId { get; private set; }
    public int DayOfWeek { get; private set; }
    public bool IsEnabled { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int SlotDurationMinutes { get; private set; }

    private AppointmentScheduleRule()
    {
    }

    private AppointmentScheduleRule(
        Guid id,
        Guid organisationId,
        Guid appointmentProfileId,
        int dayOfWeek,
        bool isEnabled,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        Id = id;
        OrganisationId = organisationId;
        AppointmentProfileId = appointmentProfileId;
        DayOfWeek = dayOfWeek;
        IsEnabled = isEnabled;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
    }

    public static AppointmentScheduleRule Create(
        Guid organisationId,
        Guid appointmentProfileId,
        int dayOfWeek,
        bool isEnabled,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        Validate(organisationId, appointmentProfileId, dayOfWeek, isEnabled, startTime, endTime, slotDurationMinutes);

        return new AppointmentScheduleRule(
            Guid.NewGuid(),
            organisationId,
            appointmentProfileId,
            dayOfWeek,
            isEnabled,
            startTime,
            endTime,
            slotDurationMinutes);
    }

    public void Configure(
        bool isEnabled,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        Validate(OrganisationId, AppointmentProfileId, DayOfWeek, isEnabled, startTime, endTime, slotDurationMinutes);

        IsEnabled = isEnabled;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
    }

    private static void Validate(
        Guid organisationId,
        Guid appointmentProfileId,
        int dayOfWeek,
        bool isEnabled,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes)
    {
        if (organisationId == Guid.Empty)
            throw new DomainException("Organisation ID is required.");

        if (appointmentProfileId == Guid.Empty)
            throw new DomainException("Appointment profile ID is required.");

        if (dayOfWeek < 0 || dayOfWeek > 6)
            throw new DomainException("DayOfWeek must be between 0 (Sunday) and 6 (Saturday).");

        if (slotDurationMinutes < 5 || slotDurationMinutes > 240)
            throw new DomainException("Slot duration must be between 5 and 240 minutes.");

        if (isEnabled && endTime <= startTime)
            throw new DomainException("End time must be after start time for enabled days.");
    }
}