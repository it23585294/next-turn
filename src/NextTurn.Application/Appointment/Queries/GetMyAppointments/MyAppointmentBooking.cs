namespace NextTurn.Application.Appointment.Queries.GetMyAppointments;

public sealed record MyAppointmentBooking(
    Guid AppointmentId,
    Guid OrganisationId,
    string OrganisationName,
    Guid AppointmentProfileId,
    string AppointmentProfileName,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd,
    string Status);
