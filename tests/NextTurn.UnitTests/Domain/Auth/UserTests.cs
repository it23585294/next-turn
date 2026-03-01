using FluentAssertions;
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
}
