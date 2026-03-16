using MediatR;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Repositories;

namespace NextTurn.Application.Organisation.Queries.ResolveOrganisationTenant;

public sealed class ResolveOrganisationTenantQueryHandler
    : IRequestHandler<ResolveOrganisationTenantQuery, ResolveOrganisationTenantResult>
{
    private readonly IOrganisationRepository _organisationRepository;

    public ResolveOrganisationTenantQueryHandler(IOrganisationRepository organisationRepository)
    {
        _organisationRepository = organisationRepository;
    }

    public async Task<ResolveOrganisationTenantResult> Handle(
        ResolveOrganisationTenantQuery request,
        CancellationToken cancellationToken)
    {
        var organisation = await _organisationRepository.GetBySlugAsync(request.Slug, cancellationToken);

        if (organisation is null)
            throw new DomainException("Organisation not found for this login slug.");

        return new ResolveOrganisationTenantResult(
            organisation.Id,
            organisation.Name,
            organisation.Slug);
    }
}
