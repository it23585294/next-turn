using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Queue.Enums;
using NextTurn.IntegrationTests;
using NextTurn.Infrastructure.Persistence;

namespace NextTurn.IntegrationTests.Queue;

/// <summary>
/// Integration tests for staff queue dashboard endpoints:
/// - GET  /api/queues/{queueId}/dashboard
/// - POST /api/queues/{queueId}/call-next
/// - POST /api/queues/{queueId}/served
/// - POST /api/queues/{queueId}/no-show
/// </summary>
[Collection("Integration")]
public sealed class StaffQueueDashboardIntegrationTests
    : IClassFixture<NextTurnWebApplicationFactory>, IAsyncLifetime
{
    private readonly NextTurnWebApplicationFactory _factory;

    private Guid _tenantId;
    private Guid _queueId;
    private Guid _staffUserId;

    private static readonly Guid UserAId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid UserBId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    public StaffQueueDashboardIntegrationTests(NextTurnWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        (_tenantId, _queueId) = await _factory.SeedQueueAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var staffUser = User.Create(
            tenantId: _tenantId,
            name: "Queue Staff",
            email: new EmailAddress("staff.integration@nextturn.dev"),
            phone: null,
            passwordHash: "integration-password-hash",
            role: UserRole.Staff);

        staffUser.Activate();

        db.Users.Add(staffUser);
        db.QueueStaffAssignments.Add(NextTurn.Domain.Queue.Entities.QueueStaffAssignment.Create(
            _tenantId,
            _queueId,
            staffUser.Id));

        await db.SaveChangesAsync();
        _staffUserId = staffUser.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDashboard_WithWaitingEntries_ReturnsCurrentAndWaitingData()
    {
        await JoinQueueAsync(UserAId);
        await JoinQueueAsync(UserBId);

        var response = await StaffClient().GetAsync($"/api/queues/{_queueId}/dashboard");
        var body = await ReadBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body["waitingCount"].GetInt32().Should().Be(2);
        body["currentlyServing"].ValueKind.Should().Be(JsonValueKind.Null);

        var waiting = body["waitingEntries"].EnumerateArray().ToList();
        waiting.Should().HaveCount(2);
        waiting[0].GetProperty("ticketNumber").GetInt32().Should().Be(1);
        waiting[1].GetProperty("ticketNumber").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task CallNext_WithWaitingEntries_SetsFirstTicketAsServing()
    {
        await JoinQueueAsync(UserAId);
        await JoinQueueAsync(UserBId);

        var callResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/call-next", null);
        var callBody = await ReadBodyAsync(callResponse);

        callResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        callBody["ticketNumber"].GetInt32().Should().Be(1);
        callBody["status"].GetString().Should().Be("Serving");

        var dashboardResponse = await StaffClient().GetAsync($"/api/queues/{_queueId}/dashboard");
        var dashboardBody = await ReadBodyAsync(dashboardResponse);

        dashboardBody["waitingCount"].GetInt32().Should().Be(1);
        dashboardBody["currentlyServing"].GetProperty("ticketNumber").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task MarkServed_WithServingEntry_ClearsCurrentServing()
    {
        await JoinQueueAsync(UserAId);
        await StaffClient().PostAsync($"/api/queues/{_queueId}/call-next", null);

        var servedResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/served", null);
        var servedBody = await ReadBodyAsync(servedResponse);

        servedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        servedBody["status"].GetString().Should().Be("Served");

        var dashboardResponse = await StaffClient().GetAsync($"/api/queues/{_queueId}/dashboard");
        var dashboardBody = await ReadBodyAsync(dashboardResponse);

        dashboardBody["currentlyServing"].ValueKind.Should().Be(JsonValueKind.Null);
        dashboardBody["waitingCount"].GetInt32().Should().Be(0);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var servedEntry = await db.QueueEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.QueueId == _queueId && e.UserId == UserAId);

        servedEntry.Should().NotBeNull();
        servedEntry!.Status.Should().Be(QueueEntryStatus.Served);
    }

    [Fact]
    public async Task CallNext_ThenMarkServed_PersistsServingToServedTransitionInDatabase()
    {
        await JoinQueueAsync(UserAId);

        var callResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/call-next", null);
        callResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using (var servingScope = _factory.Services.CreateAsyncScope())
        {
            var db = servingScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var servingEntry = await db.QueueEntries
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.QueueId == _queueId && e.UserId == UserAId);

            servingEntry.Should().NotBeNull();
            servingEntry!.Status.Should().Be(QueueEntryStatus.Serving);
        }

        var servedResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/served", null);
        servedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var servedScope = _factory.Services.CreateAsyncScope();
        var servedDb = servedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var servedEntryAfterTransition = await servedDb.QueueEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.QueueId == _queueId && e.UserId == UserAId);

        servedEntryAfterTransition.Should().NotBeNull();
        servedEntryAfterTransition!.Status.Should().Be(QueueEntryStatus.Served);
    }

    [Fact]
    public async Task MarkNoShow_WithServingEntry_ClearsCurrentServing()
    {
        await JoinQueueAsync(UserAId);
        await StaffClient().PostAsync($"/api/queues/{_queueId}/call-next", null);

        var noShowResponse = await StaffClient().PostAsync($"/api/queues/{_queueId}/no-show", null);
        var noShowBody = await ReadBodyAsync(noShowResponse);

        noShowResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        noShowBody["status"].GetString().Should().Be("NoShow");

        var dashboardResponse = await StaffClient().GetAsync($"/api/queues/{_queueId}/dashboard");
        var dashboardBody = await ReadBodyAsync(dashboardResponse);

        dashboardBody["currentlyServing"].ValueKind.Should().Be(JsonValueKind.Null);
        dashboardBody["waitingCount"].GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task CallNext_WithUserRole_Returns403Forbidden()
    {
        await JoinQueueAsync(UserAId);

        var response = await UserClient(UserBId).PostAsync($"/api/queues/{_queueId}/call-next", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private Task<HttpResponseMessage> JoinQueueAsync(Guid userId)
    {
        return UserClient(userId).PostAsync($"/api/queues/{_queueId}/join", null);
    }

    private HttpClient StaffClient()
    {
        var token = _factory.CreateTokenForRole(UserRole.Staff, userId: _staffUserId, tenantId: _tenantId);
        return AuthenticatedClient(token);
    }

    private HttpClient UserClient(Guid userId)
    {
        var token = _factory.CreateTokenForRole(UserRole.User, userId: userId, tenantId: _tenantId);
        return AuthenticatedClient(token);
    }

    private HttpClient AuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        return client;
    }

    private static async Task<Dictionary<string, JsonElement>> ReadBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
