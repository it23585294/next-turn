using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Domain.Appointment.Entities;
using NextTurn.Domain.Auth;
using NextTurn.Infrastructure.Persistence;

namespace NextTurn.IntegrationTests.Appointment;

[Collection("Integration")]
public sealed class RespawnAppointmentResetIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;
    private Guid _appointmentProfileId;

    public RespawnAppointmentResetIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        (_tenantId, _) = await _factory.SeedQueueAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var profile = AppointmentProfile.Create(_tenantId, "Respawn Reset Profile");
        db.AppointmentProfiles.Add(profile);
        await db.SaveChangesAsync();

        _appointmentProfileId = profile.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ResetDatabaseAsync_ClearsAppointmentsTable()
    {
        var client = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);
        var (slotStart, slotEnd) = SlotForTomorrow(10, 0);

        var booking = await client.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });

        booking.StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var seededScope = _factory.Services.CreateAsyncScope())
        {
            var seededDb = seededScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var seededCount = await seededDb.Appointments.IgnoreQueryFilters().CountAsync();
            seededCount.Should().Be(1);
        }

        await _factory.ResetDatabaseAsync();

        await using var resetScope = _factory.Services.CreateAsyncScope();
        var resetDb = resetScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var resetCount = await resetDb.Appointments.IgnoreQueryFilters().CountAsync();
        resetCount.Should().Be(0);
    }

    private HttpClient AuthenticatedClient(UserRole role, Guid userId, Guid tenantId)
    {
        var token = _factory.CreateTokenForRole(role, userId: userId, tenantId: tenantId);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }

    private static (DateTimeOffset SlotStart, DateTimeOffset SlotEnd) SlotForTomorrow(int hour, int minute)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var slotStart = new DateTimeOffset(date.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero);
        var slotEnd = slotStart.AddMinutes(30);
        return (slotStart, slotEnd);
    }
}
