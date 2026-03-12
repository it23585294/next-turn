using NextTurn.Domain.Common;

namespace NextTurn.Domain.Organisation.ValueObjects;

/// <summary>
/// Immutable value object representing a physical address.
/// Stored as owned-type columns on the Organisations table (no separate table).
/// Structural equality is provided by the record type.
/// </summary>
public record Address
{
    public string Line1      { get; }
    public string City       { get; }
    public string PostalCode { get; }
    public string Country    { get; }

    public Address(string line1, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(line1))
            throw new DomainException("Address line 1 is required.");

        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("City is required.");

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new DomainException("Postal code is required.");

        if (string.IsNullOrWhiteSpace(country))
            throw new DomainException("Country is required.");

        Line1      = line1.Trim();
        City       = city.Trim();
        PostalCode = postalCode.Trim();
        Country    = country.Trim();
    }

    // Parameterless constructor required by EF Core for owned-type materialisation.
    // Protected to prevent accidental use in domain code.
    protected Address()
    {
        // default! suppresses CS8618 — EF Core assigns these before the instance is ever read.
        Line1      = default!;
        City       = default!;
        PostalCode = default!;
        Country    = default!;
    }
}
