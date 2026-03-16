using FluentAssertions;
using Moq;
using NextTurn.Application.Appointment.Queries.GetAppointmentSchedule;
using NextTurn.Domain.Appointment.Entities;
using NextTurn.Domain.Appointment.Repositories;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class GetAppointmentScheduleQueryHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepositoryMock = new();

    [Fact]
    public async Task Handle_WhenNoConfiguredRules_ReturnsWeekdayDefaults()
    {
        var orgId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        _appointmentRepositoryMock
            .Setup(r => r.GetScheduleRulesAsync(orgId, profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppointmentScheduleRule>());

        var handler = new GetAppointmentScheduleQueryHandler(_appointmentRepositoryMock.Object);

        var result = await handler.Handle(new GetAppointmentScheduleQuery(orgId, profileId), CancellationToken.None);

        result.ShareableLink.Should().Be($"/appointments/{orgId}/{profileId}");
        result.DayRules.Should().HaveCount(7);
        result.DayRules.Count(r => r.IsEnabled).Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenConfiguredRuleExists_UsesConfiguredValues()
    {
        var orgId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var configured = AppointmentScheduleRule.Create(
            orgId,
            profileId,
            2,
            true,
            new TimeOnly(8, 0),
            new TimeOnly(12, 0),
            20);

        _appointmentRepositoryMock
            .Setup(r => r.GetScheduleRulesAsync(orgId, profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { configured });

        var handler = new GetAppointmentScheduleQueryHandler(_appointmentRepositoryMock.Object);

        var result = await handler.Handle(new GetAppointmentScheduleQuery(orgId, profileId), CancellationToken.None);

        result.DayRules.Should().ContainSingle(r =>
            r.DayOfWeek == 2 &&
            r.StartTime == new TimeOnly(8, 0) &&
            r.EndTime == new TimeOnly(12, 0) &&
            r.SlotDurationMinutes == 20 &&
            r.IsEnabled);
    }
}
