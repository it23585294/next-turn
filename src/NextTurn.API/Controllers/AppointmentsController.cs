using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTurn.API.Models.Appointments;
using NextTurn.Application.Appointment.Commands.BookAppointment;
using NextTurn.Application.Appointment.Commands.CancelAppointment;
using NextTurn.Application.Appointment.Commands.ConfigureAppointmentSchedule;
using NextTurn.Application.Appointment.Commands.CreateAppointmentProfile;
using NextTurn.Application.Appointment.Commands.RescheduleAppointment;
using NextTurn.Application.Appointment.Common;
using NextTurn.Application.Appointment.Queries.GetAppointmentSchedule;
using NextTurn.Application.Appointment.Queries.GetAvailableSlots;
using NextTurn.Application.Appointment.Queries.ListAppointmentProfiles;

namespace NextTurn.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize(Policy = "IsUser")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly ISender _sender;

    public AppointmentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BookAppointmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> BookAppointment(
        [FromBody] BookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var command = new BookAppointmentCommand(
            request.OrganisationId,
            request.AppointmentProfileId,
            userId,
            request.SlotStart,
            request.SlotEnd);

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("slots")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableSlot>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] Guid organisationId,
        [FromQuery] Guid appointmentProfileId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var query = new GetAvailableSlotsQuery(organisationId, appointmentProfileId, date);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("config")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(typeof(GetAppointmentScheduleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetScheduleConfig(
        [FromQuery] Guid appointmentProfileId,
        CancellationToken cancellationToken)
    {
        var tenantIdClaim = User.FindFirstValue("tid");
        if (!Guid.TryParse(tenantIdClaim, out var organisationId) || organisationId == Guid.Empty)
            return Unauthorized();

        var query = new GetAppointmentScheduleQuery(organisationId, appointmentProfileId);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpPut("config")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(typeof(ConfigureAppointmentScheduleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfigureSchedule(
        [FromQuery] Guid appointmentProfileId,
        [FromBody] ConfigureAppointmentScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var tenantIdClaim = User.FindFirstValue("tid");
        if (!Guid.TryParse(tenantIdClaim, out var organisationId) || organisationId == Guid.Empty)
            return Unauthorized();

        var command = new ConfigureAppointmentScheduleCommand(
            organisationId,
            appointmentProfileId,
            request.DayRules
                .Select(r => new AppointmentDayRuleDto(
                    r.DayOfWeek,
                    r.IsEnabled,
                    r.StartTime,
                    r.EndTime,
                    r.SlotDurationMinutes))
                .ToList());

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("profiles")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListProfiles(CancellationToken cancellationToken)
    {
        var tenantIdClaim = User.FindFirstValue("tid");
        if (!Guid.TryParse(tenantIdClaim, out var organisationId) || organisationId == Guid.Empty)
            return Unauthorized();

        var query = new ListAppointmentProfilesQuery(organisationId);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpPost("profiles")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(typeof(CreateAppointmentProfileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateProfile(
        [FromBody] CreateAppointmentProfileRequest request,
        CancellationToken cancellationToken)
    {
        var tenantIdClaim = User.FindFirstValue("tid");
        if (!Guid.TryParse(tenantIdClaim, out var organisationId) || organisationId == Guid.Empty)
            return Unauthorized();

        var command = new CreateAppointmentProfileCommand(organisationId, request.Name);
        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPut("{appointmentId:guid}/reschedule")]
    [ProducesResponseType(typeof(RescheduleAppointmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RescheduleAppointment(
        Guid appointmentId,
        [FromBody] RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var command = new RescheduleAppointmentCommand(
            appointmentId,
            userId,
            request.NewSlotStart,
            request.NewSlotEnd);

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{appointmentId:guid}/cancel")]
    [ProducesResponseType(typeof(CancelAppointmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelAppointment(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var command = new CancelAppointmentCommand(appointmentId, userId);
        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }
}
