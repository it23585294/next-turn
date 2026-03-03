namespace NextTurn.Domain.Organisation.Enums;

/// <summary>
/// Lifecycle status of an organisation on the platform.
///
/// Transition rules (enforced via domain methods):
///   PendingApproval → Active      : SystemAdmin approves (Sprint 2)
///   Active          → Suspended   : SystemAdmin suspends  (Sprint 2)
///   Suspended       → Active      : SystemAdmin reinstates (Sprint 2)
///
/// An organisation can only log in and use the platform while Active.
/// Only the initial PendingApproval status is set in Sprint 1.
/// </summary>
public enum OrganisationStatus
{
    PendingApproval,
    Active,
    Suspended,
}
