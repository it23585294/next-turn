using FluentAssertions;
using Moq;
using NextTurn.Application.Organisation.Queries.ResolveOrganisationLogin;
using NextTurn.Application.Organisation.Queries.ResolveOrganisationTenant;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.Repositories;
using NextTurn.Domain.Organisation.ValueObjects;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.UnitTests.Application.Organisation;

public sealed class ResolveOrganisationLoginAndTenantQueryHandlerTests
{
    private readonly Mock<IOrganisationRepository> _organisationRepositoryMock = new();

    [Fact]
    public async Task ResolveOrganisationLogin_WhenFound_ReturnsLoginPath()
    {
        var org = OrganisationEntity.Create(
            "Gov Services",
            "gov-services",
            new Address("1 Main", "Colombo", "10000", "LK"),
            OrganisationType.Government,
            new EmailAddress("admin@gov.lk"));

        _organisationRepositoryMock
            .Setup(r => r.GetByAdminEmailAsync("admin@gov.lk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var handler = new ResolveOrganisationLoginQueryHandler(_organisationRepositoryMock.Object);
        var result = await handler.Handle(new ResolveOrganisationLoginQuery("admin@gov.lk"), CancellationToken.None);

        result.OrganisationId.Should().Be(org.Id);
        result.LoginPath.Should().Be("/login/o/gov-services");
    }

    [Fact]
    public async Task ResolveOrganisationLogin_WhenNotFound_Throws()
    {
        _organisationRepositoryMock
            .Setup(r => r.GetByAdminEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganisationEntity?)null);

        var handler = new ResolveOrganisationLoginQueryHandler(_organisationRepositoryMock.Object);
        var act = async () => await handler.Handle(new ResolveOrganisationLoginQuery("none@example.com"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("No organisation admin account was found for this email.");
    }

    [Fact]
    public async Task ResolveOrganisationTenant_WhenFound_ReturnsTenantProjection()
    {
        var org = OrganisationEntity.Create(
            "Gov Services",
            "gov-services",
            new Address("1 Main", "Colombo", "10000", "LK"),
            OrganisationType.Government,
            new EmailAddress("admin@gov.lk"));

        _organisationRepositoryMock
            .Setup(r => r.GetBySlugAsync("gov-services", It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var handler = new ResolveOrganisationTenantQueryHandler(_organisationRepositoryMock.Object);
        var result = await handler.Handle(new ResolveOrganisationTenantQuery("gov-services"), CancellationToken.None);

        result.OrganisationId.Should().Be(org.Id);
        result.OrganisationName.Should().Be(org.Name);
        result.Slug.Should().Be("gov-services");
    }

    [Fact]
    public async Task ResolveOrganisationTenant_WhenNotFound_Throws()
    {
        _organisationRepositoryMock
            .Setup(r => r.GetBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganisationEntity?)null);

        var handler = new ResolveOrganisationTenantQueryHandler(_organisationRepositoryMock.Object);
        var act = async () => await handler.Handle(new ResolveOrganisationTenantQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Organisation not found for this login slug.");
    }
}