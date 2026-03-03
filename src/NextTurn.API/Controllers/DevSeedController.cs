using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using NextTurn.Infrastructure.Persistence;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;
using QueueEntity        = NextTurn.Domain.Queue.Entities.Queue;

namespace NextTurn.API.Controllers;

/// <summary>
/// Development-only endpoint for seeding a test organisation and queue.
///
/// This controller is ONLY exposed in the Development environment (guarded at the
/// action level). It is hidden from OpenAPI docs and never deployed to production.
///
/// Purpose:
///   Manual testing: call POST /api/dev/seed after starting the API to get a
///   ready-to-use queue link and the tenant context values needed for requests.
///
/// Idempotent: looks up seed rows by name on each call. The same IDs are returned
/// on repeated calls (rows are not re-created). IDs are stable across restarts
/// because they persist in the local dev database.
/// </summary>
[ApiController]
[Route("api/dev")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)] // hidden from Swagger / OpenAPI
public sealed class DevSeedController : ControllerBase
{
    private const string SeedOrgName   = "Dev Seed Organisation";
    private const string SeedQueueName = "General Service Queue";

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment  _env;

    public DevSeedController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db  = db;
        _env = env;
    }

    /// <summary>
    /// Seeds a test organisation and queue. Idempotent — safe to call multiple times.
    /// Returns the IDs and instructions needed to manually test the join-queue flow.
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
            return NotFound(); // endpoint does not exist outside Development

        // ── Organisation (= tenant) ───────────────────────────────────────────
        // IgnoreQueryFilters(): the global tenant filter requires a resolved tenant,
        // but we are anonymous here — skip it and query by name directly.
        var org = await _db.Organisations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Name == SeedOrgName, cancellationToken);

        if (org is null)
        {
            org = OrganisationEntity.Create(
                name:       SeedOrgName,
                address:    new Address("1 Seed Street", "Seed City", "S1 1AA", "GB"),
                type:       OrganisationType.Government,
                adminEmail: new EmailAddress("seed-admin@nextturn.dev"));

            _db.Organisations.Add(org);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // ── Queue ─────────────────────────────────────────────────────────────
        var queue = await _db.Queues
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(q => q.Name == SeedQueueName, cancellationToken);

        if (queue is null)
        {
            queue = QueueEntity.Create(
                organisationId:            org.Id,
                name:                      SeedQueueName,
                maxCapacity:               100,
                averageServiceTimeSeconds: 300); // 5 min per person

            _db.Queues.Add(queue);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            message      = "Seed data ready.",
            tenantId     = org.Id,
            queueId      = queue.Id,
            queueLink    = $"/queue/{queue.Id}",
            joinEndpoint = $"POST /api/queues/{queue.Id}/join",
            instructions = new[]
            {
                $"Set header:  X-Tenant-Id: {org.Id}",
                $"Register a user: POST /api/auth/register  (X-Tenant-Id: {org.Id})",
                $"Login:           POST /api/auth/login     (X-Tenant-Id: {org.Id})",
                $"Join queue:      POST /api/queues/{queue.Id}/join  (Authorization: Bearer <token>, X-Tenant-Id: {org.Id})"
            }
        });
    }
}

