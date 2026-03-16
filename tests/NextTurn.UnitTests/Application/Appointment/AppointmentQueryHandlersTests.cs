using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NextTurn.Application.Appointment.Queries.GetAppointmentBookingContext;
using NextTurn.Application.Appointment.Queries.GetMyAppointments;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using NextTurn.UnitTests.Helpers;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class AppointmentQueryHandlersTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task GetAppointmentBookingContext_WhenFound_ReturnsOrganisationAndProfile()
    {
        var org = OrganisationEntity.Create(
            "City Council",
            "city-council",
            new Address("1 Main", "Colombo", "10000", "LK"),
            OrganisationType.Government,
            new EmailAddress("admin@city.gov"));
        var profile = AppointmentProfile.Create(org.Id, "Passport");

        var organisations = AsyncQueryableHelper.BuildMockDbSet(new[] { org });
        var profiles = AsyncQueryableHelper.BuildMockDbSet(new[] { profile });

        _contextMock.Setup(c => c.Organisations).Returns(organisations.Object);
        _contextMock.Setup(c => c.AppointmentProfiles).Returns(profiles.Object);

        var handler = new GetAppointmentBookingContextQueryHandler(_contextMock.Object);
        var result = await handler.Handle(
            new GetAppointmentBookingContextQuery(org.Id, profile.Id),
            CancellationToken.None);

        result.OrganisationId.Should().Be(org.Id);
        result.OrganisationName.Should().Be("City Council");
        result.AppointmentProfileId.Should().Be(profile.Id);
        result.AppointmentProfileName.Should().Be("Passport");
    }

    [Fact]
    public async Task GetAppointmentBookingContext_WhenOrganisationMissing_Throws()
    {
        _contextMock.Setup(c => c.Organisations).Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<OrganisationEntity>()).Object);
        _contextMock.Setup(c => c.AppointmentProfiles).Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<AppointmentProfile>()).Object);

        var handler = new GetAppointmentBookingContextQueryHandler(_contextMock.Object);
        var act = async () => await handler.Handle(new GetAppointmentBookingContextQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Organisation not found.");
    }

    [Fact]
    public async Task GetMyAppointments_ReturnsOnlyFutureActiveAppointmentsOrderedBySlotStart()
    {
        var userId = Guid.NewGuid();
        var org = OrganisationEntity.Create(
            "City Council",
            "city-council",
            new Address("1 Main", "Colombo", "10000", "LK"),
            OrganisationType.Government,
            new EmailAddress("admin@city.gov"));
        var profile = AppointmentProfile.Create(org.Id, "Passport");

        var startA = DateTimeOffset.UtcNow.AddDays(2);
        var startB = DateTimeOffset.UtcNow.AddDays(1);
        var active1 = AppointmentEntity.Create(org.Id, profile.Id, userId, startA, startA.AddMinutes(30));
        var active2 = AppointmentEntity.Create(org.Id, profile.Id, userId, startB, startB.AddMinutes(30));
        var cancelled = AppointmentEntity.Create(org.Id, profile.Id, userId, DateTimeOffset.UtcNow.AddDays(3), DateTimeOffset.UtcNow.AddDays(3).AddMinutes(30));
        cancelled.Cancel();

        _contextMock.Setup(c => c.Appointments)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { active1, active2, cancelled }).Object);
        _contextMock.Setup(c => c.AppointmentProfiles)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { profile }).Object);
        _contextMock.Setup(c => c.Organisations)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { org }).Object);

        var handler = new GetMyAppointmentsQueryHandler(_contextMock.Object);
        var result = await handler.Handle(new GetMyAppointmentsQuery(userId), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].SlotStart.Should().Be(startB);
        result[1].SlotStart.Should().Be(startA);
        result.Select(r => r.Status).Should().OnlyContain(status => status == "Confirmed" || status == "Pending");
    }
}