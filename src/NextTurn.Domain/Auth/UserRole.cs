namespace NextTurn.Domain.Auth;

/// <summary>
/// Defines the role assigned to a user within the system.
///
/// Roles are stored on the User entity and serialised as strings in the JWT ("role" claim).
/// Role-based authorisation policies will be applied in ASP.NET Core middleware.
///
/// OrgAdmin  — manages a single organisation (its queues, staff, settings)
/// SystemAdmin — platform-level admin (manages organisations, global config)
/// </summary>
public enum UserRole
{
    User,
    Staff,
    OrgAdmin,
    SystemAdmin
}
