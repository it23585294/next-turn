namespace NextTurn.Domain.Organisation.Enums;

/// <summary>
/// The broad category of business that an organisation operates in.
/// Used during registration to classify the organisation; drives future
/// configuration defaults (e.g., default queue type, SLA settings).
/// </summary>
public enum OrganisationType
{
    Healthcare,
    Retail,
    Government,
    Education,
    Other,
}
