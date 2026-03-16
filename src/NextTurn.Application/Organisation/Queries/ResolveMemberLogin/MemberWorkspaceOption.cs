namespace NextTurn.Application.Organisation.Queries.ResolveMemberLogin;

public sealed record MemberWorkspaceOption(
    Guid OrganisationId,
    string OrganisationName,
    string Slug,
    string LoginPath,
    string Role);
