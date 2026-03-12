namespace NextTurn.Application.Common.Interfaces;

/// <summary>
/// Abstraction over password hashing. Implemented in Infrastructure using BCrypt.
/// Intentionally synchronous — BCrypt is CPU-bound, not I/O-bound.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password and returns the BCrypt hash string.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored BCrypt hash.
    /// </summary>
    bool Verify(string password, string hash);
}
