using FluentAssertions;
using NextTurn.Application.Auth.Commands.RegisterGlobalUser;

namespace NextTurn.UnitTests.Application.Auth;

/// <summary>
/// Unit tests for RegisterGlobalUserValidator.
/// Covers the same field rules as RegisterUserValidator but without the
/// special-character password rule (intentionally omitted from global registration).
/// </summary>
public sealed class RegisterGlobalUserValidatorTests
{
    private readonly RegisterGlobalUserValidator _validator = new();

    // ── Valid command baseline ────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithCompleteValidCommand_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ── Name rules ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Name_WhenEmptyOrWhitespace_HasRequiredError(string name)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Name = name });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Name) &&
            e.ErrorMessage == "Name is required.");
    }

    [Fact]
    public async Task Name_WhenExceeds100Chars_HasMaxLengthError()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { Name = new string('A', 101) });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Name) &&
            e.ErrorMessage == "Name must not exceed 100 characters.");
    }

    [Fact]
    public async Task Name_With100Chars_PassesValidation()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { Name = new string('A', 100) });

        result.Errors.Should().NotContain(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Name));
    }

    // ── Email rules ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Email_WhenEmpty_HasRequiredError()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Email = string.Empty });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Email) &&
            e.ErrorMessage == "Email is required.");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing-at-sign")]
    public async Task Email_WithInvalidFormat_HasFormatError(string invalidEmail)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Email = invalidEmail });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Email) &&
            e.ErrorMessage == "Email format is invalid.");
    }

    [Fact]
    public async Task Email_WhenExceeds254Chars_HasMaxLengthError()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { Email = new string('a', 249) + "@b.com" }); // 255 chars

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Email) &&
            e.ErrorMessage == "Email must not exceed 254 characters.");
    }

    // ── Phone rules ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Phone_WhenNull_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Phone = null });

        result.Errors.Should().NotContain(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Phone));
    }

    [Fact]
    public async Task Phone_WhenExceeds20Chars_HasMaxLengthError()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { Phone = new string('1', 21) });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Phone) &&
            e.ErrorMessage == "Phone number must not exceed 20 characters.");
    }

    // ── Password rules ────────────────────────────────────────────────────────

    [Fact]
    public async Task Password_WhenEmpty_HasRequiredError()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Password = string.Empty });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Password) &&
            e.ErrorMessage == "Password is required.");
    }

    [Fact]
    public async Task Password_WhenShorterThan8Chars_HasMinLengthError()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Password = "Ab1!" });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Password) &&
            e.ErrorMessage == "Password must be at least 8 characters.");
    }

    [Fact]
    public async Task Password_WithNoUppercaseLetter_HasUppercaseError()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Password = "alllower1!" });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Password) &&
            e.ErrorMessage == "Password must contain at least one uppercase letter.");
    }

    [Fact]
    public async Task Password_WithNoNumber_HasNumberError()
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Password = "NoNumbers!" });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Password) &&
            e.ErrorMessage == "Password must contain at least one number.");
    }

    [Theory]
    [InlineData("SecureP@ss1")]
    [InlineData("Another#Valid9")]
    [InlineData("A1aaaaaaaa")]   // exactly 8+ chars, upper + number
    public async Task Password_MeetingAllRules_HasNoPasswordErrors(string validPassword)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Password = validPassword });

        result.Errors.Should().NotContain(e =>
            e.PropertyName == nameof(RegisterGlobalUserCommand.Password));
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static RegisterGlobalUserCommand ValidCommand() => new(
        Name:     "Alice Smith",
        Email:    "alice@example.com",
        Phone:    null,
        Password: "SecureP@ss1"
    );
}
