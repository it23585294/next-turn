using NextTurn.Application.Common.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace NextTurn.Infrastructure.Auth;

/// <summary>
/// BCrypt implementation of IPasswordHasher.
///
/// Why BCrypt?
///   BCrypt is a deliberately slow, adaptive hashing algorithm designed for
///   passwords. Unlike SHA-256 or MD5, its cost factor means brute-force and
///   rainbow-table attacks are computationally expensive. The work factor can
///   be increased over time without invalidating existing hashes.
///
/// Why work factor 12?
///   OWASP recommends a minimum of 10. Factor 12 (~300ms on commodity hardware)
///   is a practical balance: negligible for a real user, prohibitive at scale
///   for an attacker. Increase to 13–14 when deployed on production hardware
///   that can absorb the extra latency.
///
/// Why synchronous?
///   BCrypt is purely CPU-bound. Wrapping it in async/await with Task.Run would
///   only add thread-pool overhead without any I/O benefit. The interface is
///   intentionally synchronous for this reason.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    // OWASP recommended minimum is 10; 12 is a sensible production default.
    private const int WorkFactor = 12;

    /// <inheritdoc/>
    public string Hash(string password)
    {
        // BCrypt.EnhancedHashPassword uses SHA-384 pre-hashing before BCrypt,
        // which safely handles passwords longer than BCrypt's 72-byte limit.
        return BC.EnhancedHashPassword(password, WorkFactor);
    }

    /// <inheritdoc/>
    public bool Verify(string password, string hash)
    {
        // Must use Enhanced variant to match the Enhanced hashing above.
        // Returns false (never throws) if the hash format is invalid — safe
        // to use directly in a boolean guard without try/catch.
        return BC.EnhancedVerify(password, hash);
    }
}
