using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Enums;

namespace NextTurn.Application.Appointment.Queries.GetMyAppointments;

public sealed class GetMyAppointmentsQueryHandler
    : IRequestHandler<GetMyAppointmentsQuery, IReadOnlyList<MyAppointmentBooking>>
{
    private readonly IApplicationDbContext _context;

    public GetMyAppointmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MyAppointmentBooking>> Handle(
        GetMyAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return await (
                from appointment in _context.Appointments
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                join profile in _context.AppointmentProfiles
                        .AsNoTracking()
                        .IgnoreQueryFilters()
                    on appointment.AppointmentProfileId equals profile.Id
                join organisation in _context.Organisations
                        .AsNoTracking()
                        .IgnoreQueryFilters()
                    on appointment.OrganisationId equals organisation.Id
                where appointment.UserId == request.UserId
                      && (appointment.Status == AppointmentStatus.Pending
                          || appointment.Status == AppointmentStatus.Confirmed)
                      && appointment.SlotEnd >= now
                orderby appointment.SlotStart
                select new MyAppointmentBooking(
                    appointment.Id,
                    organisation.Id,
                    organisation.Name,
                    profile.Id,
                    profile.Name,
                    appointment.SlotStart,
                    appointment.SlotEnd,
                    appointment.Status.ToString()))
            .ToListAsync(cancellationToken);
    }
}
