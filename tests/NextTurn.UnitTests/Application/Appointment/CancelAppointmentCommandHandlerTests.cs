using FluentAssertions;
using MediatR;
using Moq;
using NextTurn.Application.Appointment.Commands.CancelAppointment;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Repositories;
using NextTurn.Domain.Common;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class CancelAppointmentCommandHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();

    private readonly CancelAppointmentCommandHandler _handler;

    private static readonly Guid AppointmentId = Guid.NewGuid();
    private static readonly Guid OrganisationId = Guid.NewGuid();
    private static readonly Guid AppointmentProfileId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public CancelAppointmentCommandHandlerTests()
    {
        var appointment = AppointmentEntity.Create(
            OrganisationId,
            AppointmentProfileId,
            UserId,
            DateTimeOffset.UtcNow.AddHours(36),
            DateTimeOffset.UtcNow.AddHours(36).AddMinutes(30));

        _appointmentRepositoryMock
            .Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        _appointmentRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppointmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _publisherMock
            .Setup(p => p.Publish(It.IsAny<AppointmentCancelledNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CancelAppointmentCommandHandler(
            _appointmentRepositoryMock.Object,
            _contextMock.Object,
            _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_CancelsAndPublishesNotification()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.AppointmentId.Should().NotBeEmpty();
        result.LateCancellation.Should().BeFalse();

        _appointmentRepositoryMock.Verify(
            r => r.UpdateAsync(
                It.Is<AppointmentEntity>(a => a.Status == NextTurn.Domain.Appointment.Enums.AppointmentStatus.Cancelled),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(It.IsAny<AppointmentCancelledNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWrongUser_ThrowsDomainException()
    {
        var command = new CancelAppointmentCommand(AppointmentId, Guid.NewGuid());

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("You can only cancel your own appointment.");

        _appointmentRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppointmentEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPastAppointment_ThrowsDomainException()
    {
        var past = AppointmentEntity.Create(
            OrganisationId,
            AppointmentProfileId,
            UserId,
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1));

        _appointmentRepositoryMock
            .Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(past);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Past appointments cannot be cancelled.");

        _appointmentRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppointmentEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWithin24Hours_ReturnsLateCancellationTrue()
    {
        var near = AppointmentEntity.Create(
            OrganisationId,
            AppointmentProfileId,
            UserId,
            DateTimeOffset.UtcNow.AddHours(8),
            DateTimeOffset.UtcNow.AddHours(8).AddMinutes(30));

        _appointmentRepositoryMock
            .Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(near);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.LateCancellation.Should().BeTrue();
    }

    private static CancelAppointmentCommand ValidCommand() =>
        new(AppointmentId, UserId);
}
