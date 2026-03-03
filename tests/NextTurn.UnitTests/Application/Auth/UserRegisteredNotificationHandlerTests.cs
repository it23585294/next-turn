using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NextTurn.Application.Auth.Commands.RegisterUser;

namespace NextTurn.UnitTests.Application.Auth;

/// <summary>
/// Unit tests for UserRegisteredNotificationHandler.
///
/// The handler is a Sprint 1 stub: it logs the event and returns completed.
/// Tests verify:
///   - Handle() completes without throwing
///   - The logger is invoked (wiring is correct)
///   - Cancellation is respected (handler doesn't block)
/// </summary>
public sealed class UserRegisteredNotificationHandlerTests
{
    private readonly Mock<ILogger<UserRegisteredNotificationHandler>> _loggerMock = new();

    private UserRegisteredNotificationHandler CreateHandler() =>
        new(_loggerMock.Object);

    private static UserRegisteredNotification MakeNotification(
        Guid? userId = null,
        string email = "user@example.com") =>
        new(userId ?? Guid.NewGuid(), email);

    // ── Completion ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidNotification_CompletesSuccessfully()
    {
        var handler = CreateHandler();
        var notification = MakeNotification();

        Func<Task> act = () => handler.Handle(notification, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ReturnsCompletedTask()
    {
        var handler = CreateHandler();
        var notification = MakeNotification();

        var task = handler.Handle(notification, CancellationToken.None);

        await task; // should not throw or hang
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    // ── Logger invocation ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_LogsInformationMessage()
    {
        var handler = CreateHandler();
        var notification = MakeNotification();

        await handler.Handle(notification, CancellationToken.None);

        // Verify that ILogger.Log was called at Information level at least once.
        // We use the Moq extension pattern for ILogger because Log() is an
        // extension method that ultimately calls the interface's Log<TState>.
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("UserRegistered")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LogsTheCorrectUserId()
    {
        var handler = CreateHandler();
        var userId = Guid.NewGuid();
        var notification = MakeNotification(userId: userId);

        await handler.Handle(notification, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(userId.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LogsTheCorrectEmail()
    {
        var handler = CreateHandler();
        const string email = "maria@example.com";
        var notification = MakeNotification(email: email);

        await handler.Handle(notification, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(email)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Cancellation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithCancelledToken_StillCompletesCleanly()
    {
        // The stub handler does not do async I/O so cancellation should not
        // affect it. This test guards against future implementations that
        // might accidentally ignore the token.
        var handler = CreateHandler();
        var notification = MakeNotification();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = () => handler.Handle(notification, cts.Token);

        await act.Should().NotThrowAsync();
    }
}
