using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Appointment.Queries.GetAppointmentBookingContext;

public sealed class GetAppointmentBookingContextQueryHandler
    : IRequestHandler<GetAppointmentBookingContextQuery, GetAppointmentBookingContextResult>
{
    private readonly IApplicationDbContext _context;

    public GetAppointmentBookingContextQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetAppointmentBookingContextResult> Handle(
        GetAppointmentBookingContextQuery request,
        CancellationToken cancellationToken)
    {
        var organisation = await _context.Organisations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganisationId, cancellationToken);

        if (organisation is null)
            throw new DomainException("Organisation not found.");

        var profile = await _context.AppointmentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.OrganisationId == request.OrganisationId && p.Id == request.AppointmentProfileId,
                cancellationToken);

        if (profile is null)
            throw new DomainException("Appointment profile not found.");

        return new GetAppointmentBookingContextResult(
            request.OrganisationId,
            organisation.Name,
            profile.Id,
            profile.Name,
            profile.IsActive,
            profile.ShareableLink);
    }
}
