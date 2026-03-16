namespace NextTurn.API.Models.Appointments;

public sealed record BookAppointmentRequest(
    Guid OrganisationId,
    Guid AppointmentProfileId,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd);
