using FluentAssertions;
using NextTurn.Application.Auth.Commands.AcceptStaffInvite;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class AcceptStaffInviteCommandValidatorTests
{
    private readonly AcceptStaffInviteCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new AcceptStaffInviteCommand("token", "SecureP@ss1"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithMissingToken_HasTokenError()
    {
        var result = await _validator.ValidateAsync(new AcceptStaffInviteCommand("", "SecureP@ss1"));

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(AcceptStaffInviteCommand.Token) &&
            e.ErrorMessage == "Invite token is required.");
    }
}