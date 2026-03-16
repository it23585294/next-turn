using FluentAssertions;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.UnitTests.Domain.Organisation;

/// <summary>
/// Unit tests for the Organisation aggregate root and the Address value object.
/// No database, no infrastructure — pure domain logic in isolation.
/// </summary>
public sealed class OrganisationTests
{
    // ── Shared valid inputs ───────────────────────────────────────────────────

    private static readonly Address ValidAddress =
        new("123 Main St", "London", "SW1A 1AA", "UK");

    private static readonly EmailAddress ValidAdminEmail =
        new("admin@acme.com");

    private const string ValidName = "Acme Corp";

    // ── Organisation.Create — happy path ──────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ReturnsOrganisationWithCorrectName()
    {
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        org.Name.Should().Be(ValidName);
    }

    [Fact]
    public void Create_WithValidInputs_SetsStatusToPendingApproval()
    {
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        org.Status.Should().Be(OrganisationStatus.PendingApproval);
    }

    [Fact]
    public void Create_WithValidInputs_SetsCorrectType()
    {
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Retail, ValidAdminEmail);

        org.Type.Should().Be(OrganisationType.Retail);
    }

    [Fact]
    public void Create_WithValidInputs_SetsCorrectAdminEmail()
    {
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        org.AdminEmail.Should().Be(ValidAdminEmail);
    }

    [Fact]
    public void Create_WithValidInputs_SetsCorrectAddress()
    {
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        org.Address.Should().Be(ValidAddress);
    }

    [Fact]
    public void Create_GeneratesNonEmptyId()
    {
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        org.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        var org1 = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        var org2 = OrganisationEntity.Create(
            "Beta Corp", ValidAddress, OrganisationType.Retail, ValidAdminEmail);

        org1.Id.Should().NotBe(org2.Id);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);
        var after = DateTimeOffset.UtcNow;

        org.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_TrimsNameWhitespace()
    {
        var org = OrganisationEntity.Create(
            "  Acme Corp  ", ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        org.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public void Create_WithExplicitSlug_NormalizesToLowerInvariant()
    {
        var org = OrganisationEntity.Create(
            "Acme Corp",
            "  ACME-PORTAL  ",
            ValidAddress,
            OrganisationType.Healthcare,
            ValidAdminEmail);

        org.Slug.Should().Be("acme-portal");
    }

    [Fact]
    public void Create_WithoutExplicitSlug_DerivesSlugFromName()
    {
        var org = OrganisationEntity.Create(
            "Acme & Sons Ltd",
            ValidAddress,
            OrganisationType.Healthcare,
            ValidAdminEmail);

        org.Slug.Should().Be("acme-sons-ltd");
    }

    [Fact]
    public void Create_WithVeryShortName_PadsGeneratedSlug()
    {
        var org = OrganisationEntity.Create(
            "A",
            ValidAddress,
            OrganisationType.Healthcare,
            ValidAdminEmail);

        org.Slug.Length.Should().BeGreaterThanOrEqualTo(3);
    }

    // ── Organisation.Create — validation failures ─────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsDomainException(string name)
    {
        var act = () => OrganisationEntity.Create(
            name, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        act.Should().Throw<DomainException>()
           .WithMessage("Organisation name is required.");
    }

    [Fact]
    public void Create_WithNameExceeding200Chars_ThrowsDomainException()
    {
        var act = () => OrganisationEntity.Create(
            new string('A', 201), ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        act.Should().Throw<DomainException>()
           .WithMessage("Organisation name must not exceed 200 characters.");
    }

    [Fact]
    public void Create_WithNameOf200Chars_Succeeds()
    {
        var act = () => OrganisationEntity.Create(
            new string('A', 200), ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingExplicitSlug_ThrowsDomainException(string slug)
    {
        var act = () => OrganisationEntity.Create(
            ValidName, slug, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        act.Should().Throw<DomainException>()
            .WithMessage("Organisation slug is required.");
    }

    [Fact]
    public void Create_WithTooShortExplicitSlug_ThrowsDomainException()
    {
        var act = () => OrganisationEntity.Create(
            ValidName, "ab", ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        act.Should().Throw<DomainException>()
            .WithMessage("Organisation slug must be between 3 and 64 characters.");
    }

    [Fact]
    public void Create_WithTooLongExplicitSlug_ThrowsDomainException()
    {
        var act = () => OrganisationEntity.Create(
            ValidName,
            new string('a', 65),
            ValidAddress,
            OrganisationType.Healthcare,
            ValidAdminEmail);

        act.Should().Throw<DomainException>()
            .WithMessage("Organisation slug must be between 3 and 64 characters.");
    }

    // ── Address — happy path ──────────────────────────────────────────────────

    [Fact]
    public void Address_WithValidInputs_SetsAllProperties()
    {
        var address = new Address("10 Downing St", "London", "SW1A 2AA", "UK");

        address.Line1.Should().Be("10 Downing St");
        address.City.Should().Be("London");
        address.PostalCode.Should().Be("SW1A 2AA");
        address.Country.Should().Be("UK");
    }

    [Fact]
    public void Address_TrimsAllFields()
    {
        var address = new Address("  Line1  ", "  City  ", "  PC  ", "  Country  ");

        address.Line1.Should().Be("Line1");
        address.City.Should().Be("City");
        address.PostalCode.Should().Be("PC");
        address.Country.Should().Be("Country");
    }

    [Fact]
    public void Address_IsValueEqualityByFields()
    {
        var a1 = new Address("1 St", "City", "PC1", "UK");
        var a2 = new Address("1 St", "City", "PC1", "UK");

        a1.Should().Be(a2);
    }

    // ── Address — validation failures ─────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Address_WithEmptyLine1_ThrowsDomainException(string line1)
    {
        var act = () => new Address(line1, "London", "SW1A 2AA", "UK");

        act.Should().Throw<DomainException>()
           .WithMessage("Address line 1 is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Address_WithEmptyCity_ThrowsDomainException(string city)
    {
        var act = () => new Address("10 Downing St", city, "SW1A 2AA", "UK");

        act.Should().Throw<DomainException>()
           .WithMessage("City is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Address_WithEmptyPostalCode_ThrowsDomainException(string postalCode)
    {
        var act = () => new Address("10 Downing St", "London", postalCode, "UK");

        act.Should().Throw<DomainException>()
           .WithMessage("Postal code is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Address_WithEmptyCountry_ThrowsDomainException(string country)
    {
        var act = () => new Address("10 Downing St", "London", "SW1A 2AA", country);

        act.Should().Throw<DomainException>()
           .WithMessage("Country is required.");
    }

    [Fact]
    public void Approve_FromPending_SetsActive()
    {
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);

        org.Approve();

        org.Status.Should().Be(OrganisationStatus.Active);
    }

    [Fact]
    public void Approve_WhenNotPending_Throws()
    {
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);
        org.Approve();

        var act = () => org.Approve();

        act.Should().Throw<DomainException>()
            .WithMessage("Only a pending organisation can be approved.");
    }

    [Fact]
    public void Suspend_FromActive_SetsSuspended()
    {
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);
        org.Approve();

        org.Suspend();

        org.Status.Should().Be(OrganisationStatus.Suspended);
    }

    [Fact]
    public void Reinstate_FromSuspended_SetsActive()
    {
        var org = OrganisationEntity.Create(
            ValidName, ValidAddress, OrganisationType.Healthcare, ValidAdminEmail);
        org.Approve();
        org.Suspend();

        org.Reinstate();

        org.Status.Should().Be(OrganisationStatus.Active);
    }
}
