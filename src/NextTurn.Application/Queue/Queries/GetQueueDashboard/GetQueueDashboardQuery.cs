using MediatR;

namespace NextTurn.Application.Queue.Queries.GetQueueDashboard;

/// <summary>
/// Returns a staff-facing snapshot of a queue: current serving ticket and waiting line.
/// </summary>
public sealed record GetQueueDashboardQuery(Guid QueueId) : IRequest<GetQueueDashboardResult>;
