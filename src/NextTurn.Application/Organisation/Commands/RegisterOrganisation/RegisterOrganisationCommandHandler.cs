using System.Security.Cryptography;
using System.Text;
using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;
using NextTurn.Domain.Organisation.Repositories;

namespace NextTurn.Application.Organisation.Commands.RegisterOrganisation;

/// <summary>
/// Handles the RegisterOrganisationCommand — orchestrates the full org registration flow.
///
/// Steps (input validation via ValidationBehavior runs before this handler):
///   1. Verify the org is a registered business (stub always passes in Sprint 1)
///   2. Enforce unique organisation name
///   3. Construct Organisation domain object
///   4. Generate a secure 12-character temporary password
///   5. Hash the temporary password
///   6. Construct OrgAdmin User linked to the new organisation
///   7. Persist both records atomically in a single SaveChangesAsync call
///   8. Send welcome email with temporary credentials
/// </summary>
public class RegisterOrganisationCommandHandler
    : IRequestHandler<RegisterOrganisationCommand, RegisterOrganisationResult>
{
    private readonly IOrganisationRepository   _organisationRepository;
    private readonly IApplicationDbContext     _context;
    private readonly IPasswordHasher           _passwordHasher;
    private readonly IEmailService             _emailService;
    private readonly IBusinessRegistryService  _businessRegistry;

    public RegisterOrganisationCommandHandler(
        IOrganisationRepository  organisationRepository,
        IApplicationDbContext    context,
        IPasswordHasher          passwordHasher,
        IEmailService            emailService,
        IBusinessRegistryService businessRegistry)
    {
        _organisationRepository = organisationRepository;
        _context                = context;
        _passwordHasher         = passwordHasher;
        _emailService           = emailService;
        _businessRegistry       = businessRegistry;
    }

    public async Task<RegisterOrganisationResult> Handle(
        RegisterOrganisationCommand command,
        CancellationToken cancellationToken)
    {
        // Step 1 — external business registry check (stub always returns true in Sprint 1)
        bool isRegistered = await _businessRegistry.IsRegisteredBusinessAsync(
            command.OrgName, command.Country, cancellationToken);

        if (!isRegistered)
            throw new DomainException(
                "The organisation could not be verified against the business registry.");

        // Step 2 — enforce unique organisation name
        var existing = await _organisationRepository.GetByNameAsync(
            command.OrgName, cancellationToken);

        if (existing is not null)
            throw new ConflictDomainException(
                $"An organisation named '{command.OrgName}' is already registered.");

        var slug = await GenerateUniqueSlugAsync(command.OrgName, cancellationToken);

        // Step 3 — construct domain objects (invariants validated inside constructors)
        var address    = new Address(command.AddressLine1, command.City,
                                     command.PostalCode,   command.Country);

        var orgType    = Enum.Parse<OrganisationType>(command.OrgType, ignoreCase: true);
        var adminEmail = new EmailAddress(command.AdminEmail);

        var organisation = OrganisationEntity.Create(
            command.OrgName, slug, address, orgType, adminEmail);

        // Step 4 — generate a secure temporary password
        string temporaryPassword = GenerateTemporaryPassword();

        // Step 5 — hash the temporary password before it touches the database
        string passwordHash = _passwordHasher.Hash(temporaryPassword);

        // Step 6 — create the OrgAdmin user linked to the new organisation
        var adminUser = User.Create(
            tenantId:     organisation.Id,
            name:         command.AdminName,
            email:        adminEmail,
            phone:        null,
            passwordHash: passwordHash,
            role:         NextTurn.Domain.Auth.UserRole.OrgAdmin);

        // Step 7 — persist both records atomically
        // AddAsync on the repository stages the entity; SaveChangesAsync commits both.
        await _organisationRepository.AddAsync(organisation, cancellationToken);
        await _context.Users.AddAsync(adminUser, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Step 8 — send welcome email with temporary credentials
        await _emailService.SendWelcomeEmailAsync(
            toEmail:           command.AdminEmail,
            orgName:           command.OrgName,
            temporaryPassword: temporaryPassword,
            cancellationToken: cancellationToken);

        return new RegisterOrganisationResult(
            organisation.Id,
            adminUser.Id,
            $"/login/o/{organisation.Slug}");
    }

    /// <summary>
    /// Generates a cryptographically random 12-character alphanumeric temporary password.
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new char[12];
        var bytes  = RandomNumberGenerator.GetBytes(12);

        for (int i = 0; i < result.Length; i++)
            result[i] = chars[bytes[i] % chars.Length];

        return new string(result);
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string organisationName,
        CancellationToken cancellationToken)
    {
        var baseSlug = ToSlug(organisationName);

        var suffix = 0;
        while (true)
        {
            var candidate = suffix == 0 ? baseSlug : $"{baseSlug}-{suffix + 1}";
            var existing = await _organisationRepository.GetBySlugAsync(candidate, cancellationToken);

            if (existing is null)
                return candidate;

            suffix++;
        }
    }

    private static string ToSlug(string value)
    {
        var lowered = value.Trim().ToLowerInvariant();
        var sb = new StringBuilder(lowered.Length);
        var previousHyphen = false;

        foreach (var ch in lowered)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                previousHyphen = false;
                continue;
            }

            if (!previousHyphen)
            {
                sb.Append('-');
                previousHyphen = true;
            }
        }

        var slug = sb.ToString().Trim('-');
        if (slug.Length == 0)
            slug = "workspace";

        if (slug.Length < 3)
            slug = slug.PadRight(3, 'x');

        return slug.Length > 50 ? slug[..50] : slug;
    }
}
