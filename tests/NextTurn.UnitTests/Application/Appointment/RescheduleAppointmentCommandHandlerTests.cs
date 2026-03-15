using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NextTurn.Application.Appointment.Commands.RescheduleAppointment;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Repositories;
using NextTurn.Domain.Common;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class RescheduleAppointmentCommandHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();

    private readonly RescheduleAppointmentCommandHandler _handler;

    private static readonly Guid AppointmentId = Guid.NewGuid();
    private static readonly Guid OrganisationId = Guid.NewGuid();
    private static readonly Guid AppointmentProfileId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset CurrentStart = DateTimeOffset.UtcNow.AddHours(2);
    private static readonly DateTimeOffset CurrentEnd = CurrentStart.AddMinutes(30);
    private static readonly DateTimeOffset NewStart = DateTimeOffset.UtcNow.AddHours(5);
    private static readonly DateTimeOffset NewEnd = NewStart.AddMinutes(30);

    public RescheduleAppointmentCommandHandlerTests()
    {
        var current = AppointmentEntity.Create(OrganisationId, AppointmentProfileId, UserId, CurrentStart, CurrentEnd);

        _appointmentRepositoryMock
            .Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);

        _appointmentRepositoryMock
            .Setup(r => r.HasOverlapExcludingAsync(
                OrganisationId,
                AppointmentProfileId,
                NewStart,
                NewEnd,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _appointmentRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppointmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _appointmentRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AppointmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _publisherMock
            .Setup(p => p.Publish(It.IsAny<AppointmentRescheduledNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RescheduleAppointmentCommandHandler(
            _appointmentRepositoryMock.Object,
            _contextMock.Object,
            _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_UpdatesOldCreatesNewAndPublishesNotification()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.AppointmentId.Should().NotBeEmpty();
        result.SlotStart.Should().Be(NewStart);
        result.SlotEnd.Should().Be(NewEnd);

        _appointmentRepositoryMock.Verify(
            r => r.UpdateAsync(
                It.Is<AppointmentEntity>(a => a.Status == NextTurn.Domain.Appointment.Enums.AppointmentStatus.Rescheduled),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _appointmentRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<AppointmentEntity>(a =>
                    a.OrganisationId == OrganisationId &&
                    a.AppointmentProfileId == AppointmentProfileId &&
                    a.UserId == UserId &&
                    a.SlotStart == NewStart &&
                    a.SlotEnd == NewEnd),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.Publish(It.IsAny<AppointmentRescheduledNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAppointmentIsPast_ThrowsDomainException()
    {
        var pastAppointment = AppointmentEntity.Create(
            OrganisationId,
            AppointmentProfileId,
            UserId,
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1));

        _appointmentRepositoryMock
            .Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pastAppointment);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Past appointments cannot be rescheduled.");

        _appointmentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<AppointmentEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWrongUser_ThrowsDomainException()
    {
        var command = new RescheduleAppointmentCommand(
            AppointmentId,
            Guid.NewGuid(),
            NewStart,
            NewEnd);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("You can only reschedule your own appointment.");

        _appointmentRepositoryMock.Verify(r => r.HasOverlapExcludingAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNewSlotOverlaps_ThrowsConflictDomainException()
    {
        _appointmentRepositoryMock
            .Setup(r => r.HasOverlapExcludingAsync(
                OrganisationId,
                AppointmentProfileId,
                NewStart,
                NewEnd,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictDomainException>()
            .WithMessage("This time slot is already booked.");

        _appointmentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<AppointmentEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUniqueConstraintViolated_ThrowsConflictDomainException()
    {
        var dbUpdateException = new DbUpdateException(
            "Write failed",
            new Exception("Violation of unique index UX_Appointments_OrganisationId_ProfileId_SlotStart_SlotEnd_Active"));

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbUpdateException);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictDomainException>()
            .WithMessage("This time slot is already booked.");
    }

    private static RescheduleAppointmentCommand ValidCommand() =>
        new(AppointmentId, UserId, NewStart, NewEnd);
}
