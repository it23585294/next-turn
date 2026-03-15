using MediatR;

namespace NextTurn.Application.Appointment.Queries.GetAvailableSlots;

public sealed record GetAvailableSlotsQuery(
    Guid OrganisationId,
    Guid AppointmentProfileId,
    DateOnly Date) : IRequest<IReadOnlyList<AvailableSlot>>;
