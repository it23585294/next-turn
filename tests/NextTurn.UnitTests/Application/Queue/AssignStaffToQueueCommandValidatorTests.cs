using FluentAssertions;
using NextTurn.Application.Queue.Commands.AssignStaffToQueue;

namespace NextTurn.UnitTests.Application.Queue;

public sealed class AssignStaffToQueueCommandValidatorTests
{
    private readonly AssignStaffToQueueCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new AssignStaffToQueueCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyIds_HasErrors()
    {
        var result = await _validator.ValidateAsync(new AssignStaffToQueueCommand(Guid.Empty, Guid.Empty));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(AssignStaffToQueueCommand.QueueId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AssignStaffToQueueCommand.StaffUserId));
    }
}