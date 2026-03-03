using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;
using QueueEntity = NextTurn.Domain.Queue.Entities.Queue;

namespace NextTurn.Application.Queue.Commands.CreateQueue;

/// <summary>
/// Handles <see cref="CreateQueueCommand"/> — creates a new queue for an organisation.
///
/// 4-step flow:
///   1. Verify the organisation exists — DomainException if not found.
///   2. Create the Queue aggregate via the domain factory (all invariants enforced there).
///   3. Persist the new queue + save the unit of work.
///   4. Return CreateQueueResult with the QueueId and a pre-built shareable link.
///
/// Ownership is implicitly verified: OrganisationId comes from the authenticated
/// user's JWT <c>tid</c> claim (injected by the controller), so an org admin
/// can only create queues for their own organisation.
/// </summary>
public class CreateQueueCommandHandler : IRequestHandler<CreateQueueCommand, CreateQueueResult>
{
    private readonly IApplicationDbContext _context;

    public CreateQueueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateQueueResult> Handle(
        CreateQueueCommand command,
        CancellationToken  cancellationToken)
    {
        // Step 1 — verify the organisation exists
        var organisation = await _context.Organisations
            .FirstOrDefaultAsync(o => o.Id == command.OrganisationId, cancellationToken);

        if (organisation is null)
            throw new DomainException("Organisation not found.");

        // Step 2 — create the Queue domain aggregate
        // Domain invariants (name length, capacity ≥ 1, avgTime ≥ 1) are enforced here.
        var queue = QueueEntity.Create(
            organisationId:            command.OrganisationId,
            name:                      command.Name,
            maxCapacity:               command.MaxCapacity,
            averageServiceTimeSeconds: command.AverageServiceTimeSeconds);

        // Step 3 — persist
        await _context.Queues.AddAsync(queue, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Step 4 — return the result
        // ShareableLink format: /queues/{tenantId}/{queueId}
        // TenantId == OrganisationId in NextTurn's single-org-per-tenant model.
        var shareableLink = $"/queues/{command.OrganisationId}/{queue.Id}";

        return new CreateQueueResult(queue.Id, shareableLink);
    }
}
