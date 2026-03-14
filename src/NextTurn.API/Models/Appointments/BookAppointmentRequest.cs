namespace NextTurn.API.Models.Appointments;

public sealed record BookAppointmentRequest(
    Guid OrganisationId,
    DateTimeOffset SlotStart,
    DateTimeOffset SlotEnd);
