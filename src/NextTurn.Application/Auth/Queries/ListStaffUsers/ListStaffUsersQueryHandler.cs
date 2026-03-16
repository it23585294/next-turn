using MediatR;
using NextTurn.Domain.Auth.Repositories;

namespace NextTurn.Application.Auth.Queries.ListStaffUsers;

public sealed class ListStaffUsersQueryHandler
    : IRequestHandler<ListStaffUsersQuery, IReadOnlyList<StaffUserSummary>>
{
    private readonly IUserRepository _userRepository;

    public ListStaffUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<StaffUserSummary>> Handle(
        ListStaffUsersQuery request,
        CancellationToken cancellationToken)
    {
        var staffUsers = await _userRepository.ListStaffAsync(cancellationToken);

        return staffUsers
            .Select(u => new StaffUserSummary(
                u.Id,
                u.Name,
                u.Email.Value,
                u.Phone,
                u.IsActive,
                u.CreatedAt))
            .ToList();
    }
}
