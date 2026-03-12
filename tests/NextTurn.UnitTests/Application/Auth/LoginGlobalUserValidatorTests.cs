using FluentAssertions;
using NextTurn.Application.Auth.Commands.LoginGlobalUser;

namespace NextTurn.UnitTests.Application.Auth;

/// <summary>
/// Unit tests for LoginGlobalUserValidator.
/// Covers the two required fields: Email (format + required) and Password (required).
/// </summary>
public sealed class LoginGlobalUserValidatorTests
{
    private readonly LoginGlobalUserValidator _validator = new();

    // ── Valid command baseline ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithValidCommand_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ── Email rules ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Email_WhenEmpty_HasRequiredError()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Email = string.Empty });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(LoginGlobalUserCommand.Email) &&
            e.ErrorMessage == "Email is required.");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing-at-sign")]
    public async Task Email_WithInvalidFormat_HasFormatError(string invalidEmail)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Email = invalidEmail });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(LoginGlobalUserCommand.Email) &&
            e.ErrorMessage == "Email format is invalid.");
    }

    [Fact]
    public async Task Email_WithValidFormat_HasNoEmailErrors()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Email = "user@example.com" });

        result.Errors.Should().NotContain(e =>
            e.PropertyName == nameof(LoginGlobalUserCommand.Email));
    }

    // ── Password rules ────────────────────────────────────────────────────────

    [Fact]
    public async Task Password_WhenEmpty_HasRequiredError()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Password = string.Empty });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(LoginGlobalUserCommand.Password) &&
            e.ErrorMessage == "Password is required.");
    }

    [Fact]
    public async Task Password_WhenProvided_HasNoPasswordErrors()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Password = "anypassword" });

        result.Errors.Should().NotContain(e =>
            e.PropertyName == nameof(LoginGlobalUserCommand.Password));
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static LoginGlobalUserCommand ValidCommand() => new(
        Email:    "alice@example.com",
        Password: "SecureP@ss1"
    );
}
