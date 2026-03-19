using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Domain.Appointment.Entities;
using NextTurn.Domain.Appointment.Enums;
using NextTurn.Domain.Auth;
using NextTurn.Infrastructure.Persistence;

namespace NextTurn.IntegrationTests.Appointment;

[Collection("Integration")]
public sealed class CancelAppointmentIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;
    private Guid _appointmentProfileId;

    public CancelAppointmentIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        (_tenantId, _) = await _factory.SeedQueueAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var profile = AppointmentProfile.Create(_tenantId, "Cancel Flow Profile");
        db.AppointmentProfiles.Add(profile);
        await db.SaveChangesAsync();

        _appointmentProfileId = profile.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CancelAppointment_ReleasesSlotAndPersistsCancelledStatus()
    {
        var ownerId = Guid.NewGuid();
        var ownerClient = AuthenticatedClient(UserRole.User, ownerId, _tenantId);

        var date = NextBusinessDateUtc();
        var (slotStart, slotEnd) = SlotAt(date, 10, 0);

        var booking = await ownerClient.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });
        booking.StatusCode.Should().Be(HttpStatusCode.OK);

        var booked = await booking.Content.ReadFromJsonAsync<BookAppointmentApiResult>();
        booked.Should().NotBeNull();

        var cancel = await ownerClient.PostAsync($"/api/appointments/{booked!.AppointmentId}/cancel", null);
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelled = await cancel.Content.ReadFromJsonAsync<CancelAppointmentApiResult>();
        cancelled.Should().NotBeNull();
        cancelled!.AppointmentId.Should().Be(booked.AppointmentId);
        cancelled.LateCancellation.Should().BeFalse();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var persisted = await db.Appointments
            .IgnoreQueryFilters()
            .FirstAsync(a => a.Id == booked.AppointmentId);

        persisted.Status.Should().Be(AppointmentStatus.Cancelled);
        persisted.LateCancellation.Should().BeFalse();
        persisted.AppointmentProfileId.Should().Be(_appointmentProfileId);

        var dateText = date.ToString("yyyy-MM-dd");
        var slotsResponse = await ownerClient.GetAsync(
            $"/api/appointments/slots?organisationId={_tenantId}&appointmentProfileId={_appointmentProfileId}&date={dateText}");
        slotsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var slots = await slotsResponse.Content.ReadFromJsonAsync<List<SlotDto>>();
        slots.Should().NotBeNull();
        slots!.Should().Contain(s => s.SlotStart == slotStart && s.SlotEnd == slotEnd,
            "cancelled slot should become available again");

        var anotherUser = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);
        var rebook = await anotherUser.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });

        rebook.StatusCode.Should().Be(HttpStatusCode.OK,
            "slot must be bookable after cancellation");
    }

    private HttpClient AuthenticatedClient(UserRole role, Guid userId, Guid tenantId)
    {
        var token = _factory.CreateTokenForRole(role, userId: userId, tenantId: tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }

    private static (DateTimeOffset SlotStart, DateTimeOffset SlotEnd) SlotAt(DateOnly date, int hour, int minute)
    {
        var slotStart = new DateTimeOffset(date.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero);
        var slotEnd = slotStart.AddMinutes(30);
        return (slotStart, slotEnd);
    }

    private static DateOnly NextBusinessDateUtc()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private sealed record BookAppointmentApiResult(Guid AppointmentId);

    private sealed record CancelAppointmentApiResult(Guid AppointmentId, bool LateCancellation);

    private sealed record SlotDto(DateTimeOffset SlotStart, DateTimeOffset SlotEnd);
}
