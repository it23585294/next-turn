using MediatR;

namespace NextTurn.Application.Appointment.Queries.GetAvailableSlots;

public sealed record GetAvailableSlotsQuery(
    Guid OrganisationId,
    DateOnly Date) : IRequest<IReadOnlyList<AvailableSlot>>;
