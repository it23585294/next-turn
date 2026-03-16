using MediatR;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Repositories;

namespace NextTurn.Application.Organisation.Queries.ResolveOrganisationLogin;

public sealed class ResolveOrganisationLoginQueryHandler
    : IRequestHandler<ResolveOrganisationLoginQuery, ResolveOrganisationLoginResult>
{
    private readonly IOrganisationRepository _organisationRepository;

    public ResolveOrganisationLoginQueryHandler(IOrganisationRepository organisationRepository)
    {
        _organisationRepository = organisationRepository;
    }

    public async Task<ResolveOrganisationLoginResult> Handle(
        ResolveOrganisationLoginQuery request,
        CancellationToken cancellationToken)
    {
        var organisation = await _organisationRepository.GetByAdminEmailAsync(
            request.AdminEmail,
            cancellationToken);

        if (organisation is null)
            throw new DomainException("No organisation admin account was found for this email.");

        return new ResolveOrganisationLoginResult(
            organisation.Id,
            organisation.Name,
            $"/login/o/{organisation.Slug}");
    }
}
