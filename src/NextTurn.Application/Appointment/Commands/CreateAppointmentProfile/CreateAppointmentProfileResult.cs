namespace NextTurn.Application.Appointment.Commands.CreateAppointmentProfile;

public sealed record CreateAppointmentProfileResult(
    Guid AppointmentProfileId,
    string Name,
    bool IsActive,
    string ShareableLink);
