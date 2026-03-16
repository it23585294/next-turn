using FluentAssertions;
using NextTurn.Application.Auth.Commands.CreateStaffUser;

namespace NextTurn.UnitTests.Application.Auth;

public sealed class CreateStaffUserCommandValidatorTests
{
    private readonly CreateStaffUserCommandValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new CreateStaffUserCommand("Staff", "staff@example.com", null, "SecureP@ss1"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidPassword_HasExpectedErrors()
    {
        var result = await _validator.ValidateAsync(new CreateStaffUserCommand("Staff", "staff@example.com", null, "weak"));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStaffUserCommand.Password));
    }
}
