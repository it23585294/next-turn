using MediatR;
using NextTurn.Application.Appointment.Common;

namespace NextTurn.Application.Appointment.Commands.ConfigureAppointmentSchedule;

public sealed record ConfigureAppointmentScheduleCommand(
    Guid OrganisationId,
    IReadOnlyList<AppointmentDayRuleDto> DayRules)
    : IRequest<ConfigureAppointmentScheduleResult>;