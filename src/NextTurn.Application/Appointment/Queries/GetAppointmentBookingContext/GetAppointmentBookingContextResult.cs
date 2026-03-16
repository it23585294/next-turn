namespace NextTurn.Application.Appointment.Queries.GetAppointmentBookingContext;

public sealed record GetAppointmentBookingContextResult(
    Guid OrganisationId,
    string OrganisationName,
    Guid AppointmentProfileId,
    string AppointmentProfileName,
    bool IsProfileActive,
    string ShareableLink);
