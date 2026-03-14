using MediatR;
using NextTurn.Domain.Appointment.Repositories;

namespace NextTurn.Application.Appointment.Queries.GetAvailableSlots;

public sealed class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, IReadOnlyList<AvailableSlot>>
{
    private static readonly TimeOnly DayStart = new(9, 0);
    private static readonly TimeOnly DayEnd = new(17, 0);
    private static readonly TimeSpan SlotDuration = TimeSpan.FromMinutes(30);

    private readonly IAppointmentRepository _appointmentRepository;

    public GetAvailableSlotsQueryHandler(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    public async Task<IReadOnlyList<AvailableSlot>> Handle(
        GetAvailableSlotsQuery request,
        CancellationToken cancellationToken)
    {
        var booked = await _appointmentRepository.GetByOrganisationAndDateAsync(
            request.OrganisationId,
            request.Date,
            cancellationToken);

        var available = new List<AvailableSlot>();

        var startOfDay = new DateTimeOffset(request.Date.ToDateTime(DayStart), TimeSpan.Zero);
        var endOfDay = new DateTimeOffset(request.Date.ToDateTime(DayEnd), TimeSpan.Zero);

        for (var cursor = startOfDay; cursor < endOfDay; cursor = cursor.Add(SlotDuration))
        {
            var slotStart = cursor;
            var slotEnd = cursor.Add(SlotDuration);

            bool hasOverlap = booked.Any(a => a.Overlaps(slotStart, slotEnd));
            if (!hasOverlap)
            {
                available.Add(new AvailableSlot(slotStart, slotEnd));
            }
        }

        return available;
    }
}
