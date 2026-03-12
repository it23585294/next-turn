using FluentAssertions;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Domain.Auth;

/// <summary>
/// Unit tests for the EmailAddress value object.
/// EmailAddress is a record — we test construction, validation, and value equality.
/// </summary>
public sealed class EmailAddressTests
{
    // ── Happy path ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last+tag@subdomain.org")]
    [InlineData("a@b.co")]
    public void Constructor_WithValidEmail_SetsValueProperty(string validEmail)
    {
        var email = new EmailAddress(validEmail);

        email.Value.Should().Be(validEmail);
    }

    [Fact]
    public void TwoInstances_WithSameValue_AreEqual()
    {
        // EmailAddress is a record — structural equality is built in.
        var a = new EmailAddress("user@example.com");
        var b = new EmailAddress("user@example.com");

        a.Should().Be(b);
    }

    [Fact]
    public void TwoInstances_WithDifferentValues_AreNotEqual()
    {
        var a = new EmailAddress("alice@example.com");
        var b = new EmailAddress("bob@example.com");

        a.Should().NotBe(b);
    }

    // ── Validation failures ───────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithEmptyString_ThrowsDomainException()
    {
        Action act = () => new EmailAddress(string.Empty);

        act.Should().Throw<DomainException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Constructor_WithWhitespace_ThrowsDomainException()
    {
        Action act = () => new EmailAddress("   ");

        act.Should().Throw<DomainException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Constructor_WithEmailExceeding254Chars_ThrowsDomainException()
    {
        // 255-character local part — exceeds RFC 5321 limit of 254 total characters
        var tooLong = new string('a', 245) + "@b.com"; // 252 chars total > 254? Let's make it clearly over
        var overLimit = new string('a', 250) + "@b.com"; // 256 chars

        Action act = () => new EmailAddress(overLimit);

        act.Should().Throw<DomainException>()
            .WithMessage("*too long*");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void Constructor_WithInvalidFormat_ThrowsDomainException(string invalidEmail)
    {
        Action act = () => new EmailAddress(invalidEmail);

        act.Should().Throw<DomainException>()
            .WithMessage("*invalid*");
    }
}
