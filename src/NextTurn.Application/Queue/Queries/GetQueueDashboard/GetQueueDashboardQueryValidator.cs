using FluentValidation;

namespace NextTurn.Application.Queue.Queries.GetQueueDashboard;

public sealed class GetQueueDashboardQueryValidator : AbstractValidator<GetQueueDashboardQuery>
{
    public GetQueueDashboardQueryValidator()
    {
        RuleFor(x => x.QueueId)
            .NotEmpty().WithMessage("Queue ID is required.");
    }
}
