using FluentAssertions;
using Moq;
using NextTurn.Application.Appointment.Queries.GetAvailableSlots;
using NextTurn.Domain.Appointment.Repositories;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class GetAvailableSlotsQueryHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepositoryMock = new();

    private static readonly Guid OrganisationId = Guid.NewGuid();
    private static readonly Guid AppointmentProfileId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 4, 10);

    [Fact]
    public async Task Handle_WhenNoAppointments_ReturnsAllSlots()
    {
        _appointmentRepositoryMock
            .Setup(r => r.GetScheduleRulesAsync(OrganisationId, AppointmentProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule>());

        _appointmentRepositoryMock
            .Setup(r => r.GetByOrganisationAndDateAsync(OrganisationId, AppointmentProfileId, Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppointmentEntity>());

        var handler = new GetAvailableSlotsQueryHandler(_appointmentRepositoryMock.Object);

        var result = await handler.Handle(new GetAvailableSlotsQuery(OrganisationId, AppointmentProfileId, Date), CancellationToken.None);

        result.Should().HaveCount(16);
        result.Should().OnlyContain(s => s.IsBooked == false);
    }

    [Fact]
    public async Task Handle_WhenExactSlotBooked_MarksThatSlotAsBooked()
    {
        var booked = AppointmentEntity.Create(
            OrganisationId,
            AppointmentProfileId,
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 10, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 10, 30, 0, TimeSpan.Zero));

        _appointmentRepositoryMock
            .Setup(r => r.GetScheduleRulesAsync(OrganisationId, AppointmentProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule>());

        _appointmentRepositoryMock
            .Setup(r => r.GetByOrganisationAndDateAsync(OrganisationId, AppointmentProfileId, Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { booked });

        var handler = new GetAvailableSlotsQueryHandler(_appointmentRepositoryMock.Object);

        var result = await handler.Handle(new GetAvailableSlotsQuery(OrganisationId, AppointmentProfileId, Date), CancellationToken.None);

        result.Should().ContainSingle(s =>
            s.SlotStart == new DateTimeOffset(2026, 4, 10, 10, 0, 0, TimeSpan.Zero) &&
            s.SlotEnd == new DateTimeOffset(2026, 4, 10, 10, 30, 0, TimeSpan.Zero) &&
            s.IsBooked);
    }

    [Fact]
    public async Task Handle_WhenAppointmentOverlapsTwoSlots_MarksBothAsBooked()
    {
        var booked = AppointmentEntity.Create(
            OrganisationId,
            AppointmentProfileId,
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 10, 9, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 9, 45, 0, TimeSpan.Zero));

        _appointmentRepositoryMock
            .Setup(r => r.GetScheduleRulesAsync(OrganisationId, AppointmentProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<NextTurn.Domain.Appointment.Entities.AppointmentScheduleRule>());

        _appointmentRepositoryMock
            .Setup(r => r.GetByOrganisationAndDateAsync(OrganisationId, AppointmentProfileId, Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { booked });

        var handler = new GetAvailableSlotsQueryHandler(_appointmentRepositoryMock.Object);

        var result = await handler.Handle(new GetAvailableSlotsQuery(OrganisationId, AppointmentProfileId, Date), CancellationToken.None);

        result.Should().ContainSingle(s =>
            s.SlotStart == new DateTimeOffset(2026, 4, 10, 9, 0, 0, TimeSpan.Zero) &&
            s.SlotEnd == new DateTimeOffset(2026, 4, 10, 9, 30, 0, TimeSpan.Zero) &&
            s.IsBooked);

        result.Should().ContainSingle(s =>
            s.SlotStart == new DateTimeOffset(2026, 4, 10, 9, 30, 0, 0, TimeSpan.Zero) &&
            s.SlotEnd == new DateTimeOffset(2026, 4, 10, 10, 0, 0, TimeSpan.Zero) &&
            s.IsBooked);
    }
}
