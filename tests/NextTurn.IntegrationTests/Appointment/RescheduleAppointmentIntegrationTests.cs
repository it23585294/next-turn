using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using NextTurn.Domain.Auth;

namespace NextTurn.IntegrationTests.Appointment;

[Collection("Integration")]
public sealed class RescheduleAppointmentIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;

    public RescheduleAppointmentIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        (_tenantId, _) = await _factory.SeedQueueAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RescheduleAppointment_FreesOldSlotAndBooksNewSlotAtomically()
    {
        var ownerId = Guid.NewGuid();
        var ownerClient = AuthenticatedClient(UserRole.User, ownerId, _tenantId);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var (oldStart, oldEnd) = SlotAt(date, 10, 0);
        var (newStart, newEnd) = SlotAt(date, 11, 0);

        var booking = await ownerClient.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            slotStart = oldStart,
            slotEnd = oldEnd,
        });

        booking.StatusCode.Should().Be(HttpStatusCode.OK);

        var booked = await booking.Content.ReadFromJsonAsync<BookAppointmentApiResult>();
        booked.Should().NotBeNull();

        var reschedule = await ownerClient.PutAsJsonAsync($"/api/appointments/{booked!.AppointmentId}/reschedule", new
        {
            newSlotStart = newStart,
            newSlotEnd = newEnd,
        });

        reschedule.StatusCode.Should().Be(HttpStatusCode.OK);

        var rescheduled = await reschedule.Content.ReadFromJsonAsync<RescheduleAppointmentApiResult>();
        rescheduled.Should().NotBeNull();
        rescheduled!.AppointmentId.Should().NotBe(booked.AppointmentId);
        rescheduled.SlotStart.Should().Be(newStart);
        rescheduled.SlotEnd.Should().Be(newEnd);

        var dateText = date.ToString("yyyy-MM-dd");
        var slotListResponse = await ownerClient.GetAsync($"/api/appointments/slots?organisationId={_tenantId}&date={dateText}");
        slotListResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var slots = await slotListResponse.Content.ReadFromJsonAsync<List<SlotDto>>();
        slots.Should().NotBeNull();

        slots!.Should().Contain(s => s.SlotStart == oldStart && s.SlotEnd == oldEnd,
            "old slot should become available after successful reschedule");
        slots.Should().NotContain(s => s.SlotStart == newStart && s.SlotEnd == newEnd,
            "new slot should remain occupied by the replacement appointment");

        var otherUserClient = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);
        var oldSlotRebook = await otherUserClient.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            slotStart = oldStart,
            slotEnd = oldEnd,
        });

        oldSlotRebook.StatusCode.Should().Be(HttpStatusCode.OK,
            "the old slot should be freed and immediately re-bookable");
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

    private sealed record BookAppointmentApiResult(Guid AppointmentId);

    private sealed record RescheduleAppointmentApiResult(Guid AppointmentId, DateTimeOffset SlotStart, DateTimeOffset SlotEnd);

    private sealed record SlotDto(DateTimeOffset SlotStart, DateTimeOffset SlotEnd);
}
