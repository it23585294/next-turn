using FluentAssertions;
using FluentValidation;
using MediatR;
using NextTurn.Application.Common.Behaviours;

namespace NextTurn.UnitTests.Application.Common;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, Unit>(Array.Empty<IValidator<TestRequest>>());
        var nextCalled = false;

        var result = await behavior.Handle(
            new TestRequest("ok"),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult(Unit.Value);
            },
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationException()
    {
        var validators = new[] { new TestRequestValidator() };
        var behavior = new ValidationBehavior<TestRequest, Unit>(validators);

        var act = async () => await behavior.Handle(
            new TestRequest(""),
            _ => Task.FromResult(Unit.Value),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private sealed record TestRequest(string Name) : IRequest<Unit>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}