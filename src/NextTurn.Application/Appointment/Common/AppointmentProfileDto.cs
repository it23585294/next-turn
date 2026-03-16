namespace NextTurn.Application.Appointment.Common;

public sealed record AppointmentProfileDto(
    Guid AppointmentProfileId,
    string Name,
    bool IsActive,
    string ShareableLink);
