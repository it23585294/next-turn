using FluentAssertions;
using NextTurn.Domain.Appointment.Entities;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Domain.Appointment;

public sealed class AppointmentProfileTests
{
    [Fact]
    public void Create_WithValidValues_SetsDefaultsAndShareableLink()
    {
        var orgId = Guid.NewGuid();

        var profile = AppointmentProfile.Create(orgId, "  Passport Renewal  ");

        profile.OrganisationId.Should().Be(orgId);
        profile.Name.Should().Be("Passport Renewal");
        profile.IsActive.Should().BeTrue();
        profile.ShareableLink.Should().Be($"/appointments/{orgId}/{profile.Id}");
    }

    [Fact]
    public void Create_WithEmptyOrganisationId_Throws()
    {
        var act = () => AppointmentProfile.Create(Guid.Empty, "Profile");

        act.Should().Throw<DomainException>()
            .WithMessage("Organisation ID is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingName_Throws(string name)
    {
        var act = () => AppointmentProfile.Create(Guid.NewGuid(), name);

        act.Should().Throw<DomainException>()
            .WithMessage("Appointment profile name is required.");
    }

    [Fact]
    public void Rename_WithValidName_TrimsAndUpdates()
    {
        var profile = AppointmentProfile.Create(Guid.NewGuid(), "Original");

        profile.Rename("  New Name  ");

        profile.Name.Should().Be("New Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Rename_WithMissingName_Throws(string name)
    {
        var profile = AppointmentProfile.Create(Guid.NewGuid(), "Original");
        var act = () => profile.Rename(name);

        act.Should().Throw<DomainException>()
            .WithMessage("Appointment profile name is required.");
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var profile = AppointmentProfile.Create(Guid.NewGuid(), "Original");

        profile.Deactivate();

        profile.IsActive.Should().BeFalse();
    }
}