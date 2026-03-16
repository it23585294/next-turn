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
public sealed class BookAppointmentIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;
    private Guid _appointmentProfileId;

    public BookAppointmentIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        (_tenantId, _) = await _factory.SeedQueueAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var profile = AppointmentProfile.Create(_tenantId, "Integration Test Profile");
        db.AppointmentProfiles.Add(profile);
        await db.SaveChangesAsync();

        _appointmentProfileId = profile.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BookAppointment_WithValidRequest_Returns200AndAppointmentId()
    {
        var userId = Guid.NewGuid();
        var client = AuthenticatedClient(UserRole.User, userId, _tenantId);
        var (slotStart, slotEnd) = SlotForTomorrow(10, 0);

        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<BookAppointmentApiResult>();
        body.Should().NotBeNull();
        body!.AppointmentId.Should().NotBeEmpty();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var persisted = await db.Appointments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == body.AppointmentId);

        persisted.Should().NotBeNull();
        persisted!.OrganisationId.Should().Be(_tenantId);
        persisted.AppointmentProfileId.Should().Be(_appointmentProfileId);
        persisted.UserId.Should().Be(userId);
        persisted.Status.Should().Be(AppointmentStatus.Confirmed);
        persisted.SlotStart.Should().Be(slotStart);
        persisted.SlotEnd.Should().Be(slotEnd);
    }

    [Fact]
    public async Task BookAppointment_WhenSlotAlreadyBooked_Returns409Conflict()
    {
        var (slotStart, slotEnd) = SlotForTomorrow(10, 0);

        var firstClient = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);
        var secondClient = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);

        var first = await firstClient.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });

        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await secondClient.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await second.Content.ReadFromJsonAsync<ProblemDetailsPayload>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Be("This time slot is already booked.");
    }

    [Theory]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.Staff)]
    public async Task BookAppointment_UserAndOrgMemberRoles_CanBook(UserRole role)
    {
        var client = AuthenticatedClient(role, Guid.NewGuid(), _tenantId);
        var (slotStart, slotEnd) = SlotForTomorrow(role == UserRole.User ? 11 : 12, 0);

        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAvailableSlots_IsFilteredByOrganisationAndDate()
    {
        var (otherTenantId, _) = await _factory.SeedQueueAsync();
        Guid otherProfileId;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var otherProfile = AppointmentProfile.Create(otherTenantId, "Other Org Profile");
            db.AppointmentProfiles.Add(otherProfile);
            await db.SaveChangesAsync();
            otherProfileId = otherProfile.Id;
        }

        var (slotStart, slotEnd) = SlotForTomorrow(14, 0);

        var bookingClient = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);
        var booking = await bookingClient.PostAsJsonAsync("/api/appointments", new
        {
            organisationId = _tenantId,
            appointmentProfileId = _appointmentProfileId,
            slotStart,
            slotEnd,
        });
        booking.StatusCode.Should().Be(HttpStatusCode.OK);

        var date = DateOnly.FromDateTime(slotStart.UtcDateTime).ToString("yyyy-MM-dd");

        var orgAClient = AuthenticatedClient(UserRole.User, Guid.NewGuid(), _tenantId);
        var orgASlotsResponse = await orgAClient.GetAsync($"/api/appointments/slots?organisationId={_tenantId}&appointmentProfileId={_appointmentProfileId}&date={date}");
        orgASlotsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orgASlots = await orgASlotsResponse.Content.ReadFromJsonAsync<List<SlotDto>>();
        orgASlots.Should().NotBeNull();
        orgASlots!.Should().Contain(s => s.SlotStart == slotStart && s.IsBooked,
            "booked slot should be returned as unavailable (IsBooked=true)");

        var orgBClient = AuthenticatedClient(UserRole.User, Guid.NewGuid(), otherTenantId);
        var orgBSlotsResponse = await orgBClient.GetAsync($"/api/appointments/slots?organisationId={otherTenantId}&appointmentProfileId={otherProfileId}&date={date}");
        orgBSlotsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orgBSlots = await orgBSlotsResponse.Content.ReadFromJsonAsync<List<SlotDto>>();
        orgBSlots.Should().NotBeNull();
        orgBSlots!.Should().Contain(s => s.SlotStart == slotStart && !s.IsBooked,
            "the same slot in another organisation should remain available");
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

    private sealed record BookAppointmentApiResult(Guid AppointmentId);

    private sealed record ProblemDetailsPayload(string Detail);

    private sealed record SlotDto(DateTimeOffset SlotStart, DateTimeOffset SlotEnd, bool IsBooked);
}
