namespace NextTurn.Domain.Common;

/// <summary>
/// Thrown when a command violates a uniqueness constraint — for example,
/// registering an organisation with a name that already exists.
/// Caught by <c>DomainExceptionMiddleware</c> and mapped to HTTP 409 Conflict.
/// </summary>
public class ConflictDomainException : DomainException
{
    public ConflictDomainException(string message) : base(message) { }
}
