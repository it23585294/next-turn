using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NextTurn.Application.Auth.Commands.RegisterGlobalUser;

namespace NextTurn.UnitTests.Application.Auth;

/// <summary>
/// Unit tests for RegisterGlobalUserNotificationHandler.
///
/// The handler is a Sprint stub: it logs the event and returns completed.
/// Tests verify:
///   - Handle() completes without throwing
///   - The logger is invoked (wiring is correct)
///   - UserId and Email appear in the log output
///   - Cancellation is respected (handler doesn't block)
/// </summary>
public sealed class RegisterGlobalUserNotificationHandlerTests
{
    private readonly Mock<ILogger<RegisterGlobalUserNotificationHandler>> _loggerMock = new();

    private RegisterGlobalUserNotificationHandler CreateHandler() =>
        new(_loggerMock.Object);

    private static RegisterGlobalUserNotification MakeNotification(
        Guid?  userId = null,
        string email  = "user@example.com") =>
        new(userId ?? Guid.NewGuid(), email);

    // ── Completion ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidNotification_CompletesSuccessfully()
    {
        var handler      = CreateHandler();
        var notification = MakeNotification();

        Func<Task> act = () => handler.Handle(notification, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ReturnsCompletedTask()
    {
        var handler      = CreateHandler();
        var notification = MakeNotification();

        var task = handler.Handle(notification, CancellationToken.None);

        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    // ── Logger invocation ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_LogsInformationMessage()
    {
        var handler      = CreateHandler();
        var notification = MakeNotification();

        await handler.Handle(notification, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Consumer account registered")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LogsTheCorrectUserId()
    {
        var handler  = CreateHandler();
        var userId   = Guid.NewGuid();
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
        var handler      = CreateHandler();
        const string email = "consumer@example.com";
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
        var handler      = CreateHandler();
        var notification = MakeNotification();
        using var cts    = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = () => handler.Handle(notification, cts.Token);

        await act.Should().NotThrowAsync();
    }
}
