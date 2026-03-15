using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NextTurn.Application.Appointment.Commands.BookAppointment;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Appointment.Repositories;
using NextTurn.Domain.Common;
using AppointmentEntity = NextTurn.Domain.Appointment.Entities.Appointment;

namespace NextTurn.UnitTests.Application.Appointment;

public sealed class BookAppointmentCommandHandlerTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    private readonly BookAppointmentCommandHandler _handler;

    private static readonly Guid OrganisationId = Guid.NewGuid();
    private static readonly Guid AppointmentProfileId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset SlotStart = new(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SlotEnd = new(2026, 4, 2, 10, 30, 0, TimeSpan.Zero);

    public BookAppointmentCommandHandlerTests()
    {
        _appointmentRepositoryMock
            .Setup(r => r.HasOverlapAsync(OrganisationId, AppointmentProfileId, SlotStart, SlotEnd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _appointmentRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AppointmentEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new BookAppointmentCommandHandler(_appointmentRepositoryMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsAppointmentId()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.AppointmentId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsAndSaves()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        _appointmentRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<AppointmentEntity>(a =>
                    a.OrganisationId == OrganisationId &&
                    a.AppointmentProfileId == AppointmentProfileId &&
                    a.UserId == UserId &&
                    a.SlotStart == SlotStart &&
                    a.SlotEnd == SlotEnd),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSlotOverlaps_ThrowsConflictDomainException()
    {
        _appointmentRepositoryMock
            .Setup(r => r.HasOverlapAsync(OrganisationId, AppointmentProfileId, SlotStart, SlotEnd, It.IsAny<CancellationToken>()))
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

    [Fact]
    public async Task Handle_WhenOtherDbUpdateException_Propagates()
    {
        var dbUpdateException = new DbUpdateException(
            "Write failed",
            new Exception("deadlock"));

        _contextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbUpdateException);

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    private static BookAppointmentCommand ValidCommand() =>
        new(OrganisationId, AppointmentProfileId, UserId, SlotStart, SlotEnd);
}
