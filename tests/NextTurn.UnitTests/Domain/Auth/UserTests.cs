using FluentAssertions;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Domain.Auth;

/// <summary>
/// Unit tests for the User aggregate root.
/// Covers the factory method, property initialisation, and behaviour methods.
/// </summary>
public sealed class UserTests
{
    // ── Shared valid inputs ───────────────────────────────────────────────────

    private static readonly Guid ValidTenantId = Guid.NewGuid();
    private static readonly EmailAddress ValidEmail = new("alice@example.com");
    private const string ValidName = "Alice Smith";
    private const string ValidPasswordHash = "$2a$12$validbcrypthashvalue";

    // ── User.Create happy path ────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ReturnsUserWithCorrectProperties()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, "+1234567890", ValidPasswordHash);

        user.TenantId.Should().Be(ValidTenantId);
        user.Name.Should().Be(ValidName);
        user.Email.Should().Be(ValidEmail);
        user.Phone.Should().Be("+1234567890");
        user.PasswordHash.Should().Be(ValidPasswordHash);
    }

    [Fact]
    public void Create_SetsIsActiveToTrue()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);
        var after = DateTimeOffset.UtcNow;

        user.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_GeneratesNonEmptyId()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        var user1 = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);
        var user2 = User.Create(ValidTenantId, ValidName, new EmailAddress("bob@example.com"), null, ValidPasswordHash);

        user1.Id.Should().NotBe(user2.Id);
    }

    [Fact]
    public void Create_WithNullPhone_SetsPhoneToNull()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.Phone.Should().BeNull();
    }

    // ── User.Create validation failures ──────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsDomainException(string name)
    {
        Action act = () => User.Create(ValidTenantId, name, ValidEmail, null, ValidPasswordHash);

        act.Should().Throw<DomainException>()
            .WithMessage("*Name*required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyPasswordHash_ThrowsDomainException(string hash)
    {
        Action act = () => User.Create(ValidTenantId, ValidName, ValidEmail, null, hash);

        act.Should().Throw<DomainException>()
            .WithMessage("*Password hash*required*");
    }

    // ── Behaviour methods ─────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_SetsIsActiveToTrue()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);
        user.Deactivate();

        user.Activate();

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_RemainsActive()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.Activate(); // no-op
        
        user.IsActive.Should().BeTrue();
    }

    // ── Role & security initial state ─────────────────────────────────────────

    [Fact]
    public void Create_WithNoRoleSpecified_DefaultsToUserRole()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.Role.Should().Be(UserRole.User);
    }

    [Theory]
    [InlineData(UserRole.Staff)]
    [InlineData(UserRole.OrgAdmin)]
    [InlineData(UserRole.SystemAdmin)]
    public void Create_WithExplicitRole_SetsRoleCorrectly(UserRole role)
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, role);

        user.Role.Should().Be(role);
    }

    [Fact]
    public void Create_SetsInitialFailedLoginAttemptsToZero()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void Create_SetsLockoutUntilToNull()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.LockoutUntil.Should().BeNull();
    }

    [Fact]
    public void Create_SetsMfaEnabledToFalse()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.MfaEnabled.Should().BeFalse();
    }

    // ── RecordFailedLogin ─────────────────────────────────────────────────────

    [Fact]
    public void RecordFailedLogin_FirstAttempt_IncrementsCounterToOne()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.RecordFailedLogin();

        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public void RecordFailedLogin_BelowThreshold_DoesNotLockAccount()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.RecordFailedLogin(); // attempt 1
        user.RecordFailedLogin(); // attempt 2

        user.IsLockedOut().Should().BeFalse();
        user.LockoutUntil.Should().BeNull();
    }

    [Fact]
    public void RecordFailedLogin_OnThirdAttempt_LocksAccount()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.RecordFailedLogin(); // 1
        user.RecordFailedLogin(); // 2
        user.RecordFailedLogin(); // 3 — threshold reached

        user.IsLockedOut().Should().BeTrue();
        user.LockoutUntil.Should().NotBeNull();
    }

    [Fact]
    public void RecordFailedLogin_OnThirdAttempt_LockoutIsApproximatelyTenMinutes()
    {
        var before = DateTimeOffset.UtcNow.AddMinutes(10);
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.RecordFailedLogin();
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        var after = DateTimeOffset.UtcNow.AddMinutes(10);

        user.LockoutUntil.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    // ── IsLockedOut ───────────────────────────────────────────────────────────

    [Fact]
    public void IsLockedOut_WhenNotLocked_ReturnsFalse()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.IsLockedOut().Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_WhenLockoutIsInFuture_ReturnsTrue()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.Lock(TimeSpan.FromMinutes(10));

        user.IsLockedOut().Should().BeTrue();
    }

    [Fact]
    public void IsLockedOut_WhenLockoutHasExpired_ReturnsFalse()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);
        // Lock with a duration that is already in the past by passing a negative span
        user.Lock(TimeSpan.FromMilliseconds(-1));

        user.IsLockedOut().Should().BeFalse();
    }

    // ── Lock / Unlock ─────────────────────────────────────────────────────────

    [Fact]
    public void Lock_SetsLockoutUntilToFutureTime()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);

        user.Lock(TimeSpan.FromMinutes(5));

        user.LockoutUntil.Should().NotBeNull();
        user.LockoutUntil!.Value.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Unlock_ResetsFailedAttemptsToZeroAndClearsLockout()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash);
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        user.RecordFailedLogin(); // now locked

        user.Unlock();

        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutUntil.Should().BeNull();
        user.IsLockedOut().Should().BeFalse();
    }

    [Fact]
    public void StartStaffInvite_ForStaffUser_SetsInviteFieldsAndDeactivates()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.Staff);

        user.StartStaffInvite("ABC123", DateTimeOffset.UtcNow.AddHours(2));

        user.StaffInviteTokenHash.Should().Be("ABC123");
        user.StaffInviteExpiresAt.Should().NotBeNull();
        user.IsActive.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutUntil.Should().BeNull();
    }

    [Fact]
    public void StartStaffInvite_ForNonStaff_Throws()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.User);
        var act = () => user.StartStaffInvite("ABC123", DateTimeOffset.UtcNow.AddHours(2));

        act.Should().Throw<DomainException>()
            .WithMessage("Only staff users can have invite tokens.");
    }

    [Fact]
    public void StartStaffInvite_WithMissingTokenHash_Throws()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.Staff);
        var act = () => user.StartStaffInvite(" ", DateTimeOffset.UtcNow.AddHours(2));

        act.Should().Throw<DomainException>()
            .WithMessage("Invite token hash is required.");
    }

    [Fact]
    public void StartStaffInvite_WithPastExpiry_Throws()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.Staff);
        var act = () => user.StartStaffInvite("ABC123", DateTimeOffset.UtcNow.AddMinutes(-1));

        act.Should().Throw<DomainException>()
            .WithMessage("Invite expiry must be in the future.");
    }

    [Fact]
    public void HasActiveInviteToken_WithMatchingTokenAndFutureExpiry_ReturnsTrue()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.Staff);
        user.StartStaffInvite("ABC123", DateTimeOffset.UtcNow.AddHours(1));

        user.HasActiveInviteToken("ABC123").Should().BeTrue();
    }

    [Fact]
    public void HasActiveInviteToken_WithMismatchedToken_ReturnsFalse()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.Staff);
        user.StartStaffInvite("ABC123", DateTimeOffset.UtcNow.AddHours(1));

        user.HasActiveInviteToken("NOPE").Should().BeFalse();
    }

    [Fact]
    public void HasActiveInviteToken_WithExpiredInvite_ReturnsFalse()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.Staff);
        user.StartStaffInvite("ABC123", DateTimeOffset.UtcNow.AddHours(1));
        user.AcceptStaffInvite("newhash");

        user.HasActiveInviteToken("ABC123").Should().BeFalse();
    }

    [Fact]
    public void AcceptStaffInvite_WithValidInvite_ActivatesUserAndClearsInvite()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.Staff);
        user.StartStaffInvite("ABC123", DateTimeOffset.UtcNow.AddHours(1));

        user.AcceptStaffInvite("final-hash");

        user.IsActive.Should().BeTrue();
        user.PasswordHash.Should().Be("final-hash");
        user.StaffInviteTokenHash.Should().BeNull();
        user.StaffInviteExpiresAt.Should().BeNull();
    }

    [Fact]
    public void AcceptStaffInvite_ForNonStaff_Throws()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.User);
        var act = () => user.AcceptStaffInvite("hash");

        act.Should().Throw<DomainException>()
            .WithMessage("Only staff users can accept staff invites.");
    }

    [Fact]
    public void AcceptStaffInvite_WithMissingPasswordHash_Throws()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.Staff);
        user.StartStaffInvite("ABC123", DateTimeOffset.UtcNow.AddHours(1));

        var act = () => user.AcceptStaffInvite(" ");

        act.Should().Throw<DomainException>()
            .WithMessage("Password hash is required.");
    }

    [Fact]
    public void AcceptStaffInvite_WithoutInvite_Throws()
    {
        var user = User.Create(ValidTenantId, ValidName, ValidEmail, null, ValidPasswordHash, UserRole.Staff);
        var act = () => user.AcceptStaffInvite("hash");

        act.Should().Throw<DomainException>()
            .WithMessage("No active invite was found for this account.");
    }
}
