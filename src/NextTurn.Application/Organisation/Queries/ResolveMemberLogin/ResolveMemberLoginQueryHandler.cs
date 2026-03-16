using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth;

namespace NextTurn.Application.Organisation.Queries.ResolveMemberLogin;

public sealed class ResolveMemberLoginQueryHandler
    : IRequestHandler<ResolveMemberLoginQuery, IReadOnlyList<MemberWorkspaceOption>>
{
    private readonly IApplicationDbContext _context;

    public ResolveMemberLoginQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MemberWorkspaceOption>> Handle(
        ResolveMemberLoginQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var rows = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Email.Value.ToLower() == normalizedEmail)
            .Where(u => u.Role == UserRole.Staff || u.Role == UserRole.OrgAdmin || u.Role == UserRole.SystemAdmin)
            .Join(
                _context.Organisations.IgnoreQueryFilters(),
                user => user.TenantId,
                org => org.Id,
                (user, org) => new
                {
                    org.Id,
                    org.Name,
                    org.Slug,
                    Role = user.Role.ToString(),
                })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new MemberWorkspaceOption(
                x.Id,
                x.Name,
                x.Slug,
                $"/login/o/{x.Slug}",
                x.Role))
            .ToList();
    }
}
