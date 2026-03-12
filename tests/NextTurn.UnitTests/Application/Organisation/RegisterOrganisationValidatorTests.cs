using FluentAssertions;
using NextTurn.Application.Organisation.Commands.RegisterOrganisation;

namespace NextTurn.UnitTests.Application.Organisation;

/// <summary>
/// Unit tests for RegisterOrganisationCommandValidator.
/// Validates each field rule in isolation using ValidateAsync directly.
/// </summary>
public sealed class RegisterOrganisationValidatorTests
{
    private readonly RegisterOrganisationCommandValidator _validator = new();

    // ── Valid baseline ────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithAllValidFields_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ── OrgName ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OrgName_WhenEmptyOrWhitespace_HasRequiredError(string orgName)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { OrgName = orgName });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.OrgName) &&
            e.ErrorMessage == "Organisation name is required.");
    }

    [Fact]
    public async Task OrgName_WhenExceeds200Chars_HasMaxLengthError()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { OrgName = new string('A', 201) });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.OrgName) &&
            e.ErrorMessage == "Organisation name must not exceed 200 characters.");
    }

    [Fact]
    public async Task OrgName_With200Chars_PassesValidation()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { OrgName = new string('A', 200) });

        result.Errors.Should().NotContain(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.OrgName));
    }

    // ── AddressLine1 ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddressLine1_WhenEmptyOrWhitespace_HasRequiredError(string line1)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { AddressLine1 = line1 });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.AddressLine1) &&
            e.ErrorMessage == "Address line 1 is required.");
    }

    [Fact]
    public async Task AddressLine1_WhenExceeds300Chars_HasMaxLengthError()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { AddressLine1 = new string('A', 301) });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.AddressLine1) &&
            e.ErrorMessage == "Address line 1 must not exceed 300 characters.");
    }

    // ── City ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task City_WhenEmptyOrWhitespace_HasRequiredError(string city)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { City = city });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.City) &&
            e.ErrorMessage == "City is required.");
    }

    [Fact]
    public async Task City_WhenExceeds100Chars_HasMaxLengthError()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { City = new string('A', 101) });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.City) &&
            e.ErrorMessage == "City must not exceed 100 characters.");
    }

    // ── PostalCode ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostalCode_WhenEmptyOrWhitespace_HasRequiredError(string postalCode)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { PostalCode = postalCode });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.PostalCode) &&
            e.ErrorMessage == "Postal code is required.");
    }

    [Fact]
    public async Task PostalCode_WhenExceeds20Chars_HasMaxLengthError()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { PostalCode = new string('A', 21) });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.PostalCode) &&
            e.ErrorMessage == "Postal code must not exceed 20 characters.");
    }

    // ── Country ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Country_WhenEmptyOrWhitespace_HasRequiredError(string country)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { Country = country });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.Country) &&
            e.ErrorMessage == "Country is required.");
    }

    [Fact]
    public async Task Country_WhenExceeds100Chars_HasMaxLengthError()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { Country = new string('A', 101) });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.Country) &&
            e.ErrorMessage == "Country must not exceed 100 characters.");
    }

    // ── OrgType ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OrgType_WhenEmptyOrWhitespace_HasRequiredError(string orgType)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { OrgType = orgType });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.OrgType) &&
            e.ErrorMessage == "Organisation type is required.");
    }

    [Theory]
    [InlineData("InvalidType")]
    [InlineData("hospital")]
    [InlineData("Clinic")]
    public async Task OrgType_WithUnknownValue_HasInvalidEnumError(string orgType)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { OrgType = orgType });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.OrgType) &&
            e.ErrorMessage.Contains("Organisation type must be one of:"));
    }

    [Theory]
    [InlineData("Healthcare")]
    [InlineData("Retail")]
    [InlineData("Government")]
    [InlineData("Education")]
    [InlineData("Other")]
    public async Task OrgType_WithValidEnumValue_PassesValidation(string orgType)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { OrgType = orgType });

        result.Errors.Should().NotContain(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.OrgType));
    }

    [Theory]
    [InlineData("healthcare")]
    [InlineData("HEALTHCARE")]
    [InlineData("rEtAiL")]
    public async Task OrgType_WithValidValueDifferentCase_PassesValidation(string orgType)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { OrgType = orgType });

        result.Errors.Should().NotContain(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.OrgType));
    }

    // ── AdminName ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AdminName_WhenEmptyOrWhitespace_HasRequiredError(string adminName)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { AdminName = adminName });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.AdminName) &&
            e.ErrorMessage == "Admin name is required.");
    }

    [Fact]
    public async Task AdminName_WhenExceeds200Chars_HasMaxLengthError()
    {
        var result = await _validator.ValidateAsync(
            ValidCommand() with { AdminName = new string('A', 201) });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.AdminName) &&
            e.ErrorMessage == "Admin name must not exceed 200 characters.");
    }

    // ── AdminEmail ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AdminEmail_WhenEmptyOrWhitespace_HasRequiredError(string adminEmail)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { AdminEmail = adminEmail });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.AdminEmail) &&
            e.ErrorMessage == "Admin email is required.");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public async Task AdminEmail_WithInvalidFormat_HasFormatError(string adminEmail)
    {
        var result = await _validator.ValidateAsync(ValidCommand() with { AdminEmail = adminEmail });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.AdminEmail) &&
            e.ErrorMessage == "Admin email format is invalid.");
    }

    [Fact]
    public async Task AdminEmail_WhenExceeds320Chars_HasMaxLengthError()
    {
        // 321 chars: "a@b." + 317 c's — valid format per FluentValidation regex, exceeds max length
        var longEmail = "a@b." + new string('c', 317);

        var result = await _validator.ValidateAsync(
            ValidCommand() with { AdminEmail = longEmail });

        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(RegisterOrganisationCommand.AdminEmail) &&
            e.ErrorMessage == "Admin email must not exceed 320 characters.");
    }

    // ── Multiple field failures ───────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithMultipleInvalidFields_ReturnsAllErrors()
    {
        var command = new RegisterOrganisationCommand(
            OrgName:      "",
            AddressLine1: "",
            City:         "",
            PostalCode:   "",
            Country:      "",
            OrgType:      "",
            AdminName:    "",
            AdminEmail:   "");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(8);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static RegisterOrganisationCommand ValidCommand() =>
        new(
            OrgName:      "Acme Corp",
            AddressLine1: "123 Main St",
            City:         "London",
            PostalCode:   "SW1A 1AA",
            Country:      "UK",
            OrgType:      "Healthcare",
            AdminName:    "Jane Smith",
            AdminEmail:   "admin@acme.com");
}
