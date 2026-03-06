using MediatR;

namespace NextTurn.Application.Auth.Commands.RegisterGlobalUser;

/// <summary>
/// Command for registering a consumer (end-user) account that is NOT bound to
/// any specific organisation.  The resulting User will have TenantId = Guid.Empty,
/// which lets them join queues across any org without being scoped to one tenant.
/// </summary>
public record RegisterGlobalUserCommand(
    string Name,
    string Email,
    string? Phone,
    string Password
) : IRequest<Unit>;
