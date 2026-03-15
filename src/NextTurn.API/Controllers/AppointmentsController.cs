using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTurn.API.Models.Appointments;
using NextTurn.Application.Appointment.Commands.BookAppointment;
using NextTurn.Application.Appointment.Commands.CancelAppointment;
using NextTurn.Application.Appointment.Commands.RescheduleAppointment;
using NextTurn.Application.Appointment.Queries.GetAvailableSlots;

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
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var query = new GetAvailableSlotsQuery(organisationId, date);
        var result = await _sender.Send(query, cancellationToken);

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
