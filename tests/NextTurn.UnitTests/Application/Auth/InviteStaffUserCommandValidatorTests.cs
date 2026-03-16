using FluentAssertions;
using NextTurn.Application.Auth.Commands.InviteStaffUser;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class InviteStaffUserCommandValidatorTests
{
    private readonly InviteStaffUserCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new InviteStaffUserCommand("Staff", "staff@example.com", null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidEmail_HasEmailError()
    {
        var result = await _validator.ValidateAsync(new InviteStaffUserCommand("Staff", "bad-email", null));

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(InviteStaffUserCommand.Email) &&
            e.ErrorMessage == "Email format is invalid.");
    }
}