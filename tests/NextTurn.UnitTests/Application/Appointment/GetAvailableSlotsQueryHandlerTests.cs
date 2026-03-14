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
    private static readonly DateOnly Date = new(2026, 4, 10);

    [Fact]
    public async Task Handle_WhenNoAppointments_ReturnsAllSlots()
    {
        _appointmentRepositoryMock
            .Setup(r => r.GetByOrganisationAndDateAsync(OrganisationId, Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AppointmentEntity>());

        var handler = new GetAvailableSlotsQueryHandler(_appointmentRepositoryMock.Object);

        var result = await handler.Handle(new GetAvailableSlotsQuery(OrganisationId, Date), CancellationToken.None);

        result.Should().HaveCount(16);
    }

    [Fact]
    public async Task Handle_WhenExactSlotBooked_RemovesThatSlot()
    {
        var booked = AppointmentEntity.Create(
            OrganisationId,
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 10, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 10, 30, 0, TimeSpan.Zero));

        _appointmentRepositoryMock
            .Setup(r => r.GetByOrganisationAndDateAsync(OrganisationId, Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { booked });

        var handler = new GetAvailableSlotsQueryHandler(_appointmentRepositoryMock.Object);

        var result = await handler.Handle(new GetAvailableSlotsQuery(OrganisationId, Date), CancellationToken.None);

        result.Should().NotContain(s =>
            s.SlotStart == new DateTimeOffset(2026, 4, 10, 10, 0, 0, TimeSpan.Zero) &&
            s.SlotEnd == new DateTimeOffset(2026, 4, 10, 10, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_WhenAppointmentOverlapsTwoSlots_RemovesBothSlots()
    {
        var booked = AppointmentEntity.Create(
            OrganisationId,
            Guid.NewGuid(),
            new DateTimeOffset(2026, 4, 10, 9, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 9, 45, 0, TimeSpan.Zero));

        _appointmentRepositoryMock
            .Setup(r => r.GetByOrganisationAndDateAsync(OrganisationId, Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { booked });

        var handler = new GetAvailableSlotsQueryHandler(_appointmentRepositoryMock.Object);

        var result = await handler.Handle(new GetAvailableSlotsQuery(OrganisationId, Date), CancellationToken.None);

        result.Should().NotContain(s =>
            s.SlotStart == new DateTimeOffset(2026, 4, 10, 9, 0, 0, TimeSpan.Zero) &&
            s.SlotEnd == new DateTimeOffset(2026, 4, 10, 9, 30, 0, TimeSpan.Zero));

        result.Should().NotContain(s =>
            s.SlotStart == new DateTimeOffset(2026, 4, 10, 9, 30, 0, TimeSpan.Zero) &&
            s.SlotEnd == new DateTimeOffset(2026, 4, 10, 10, 0, 0, TimeSpan.Zero));
    }
}
