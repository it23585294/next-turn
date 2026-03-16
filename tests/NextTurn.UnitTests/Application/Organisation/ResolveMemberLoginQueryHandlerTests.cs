using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Organisation.Queries.ResolveMemberLogin;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Organisation.Enums;
using NextTurn.Domain.Organisation.ValueObjects;
using NextTurn.UnitTests.Helpers;
using OrganisationEntity = NextTurn.Domain.Organisation.Entities.Organisation;

namespace NextTurn.UnitTests.Application.Organisation;

public sealed class ResolveMemberLoginQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();

    [Fact]
    public async Task Handle_WithMatchingMemberRoles_ReturnsWorkspaceOptions()
    {
        var orgA = OrganisationEntity.Create(
            "City Council",
            "city-council",
            new Address("1 Main", "Colombo", "10000", "LK"),
            OrganisationType.Government,
            new EmailAddress("admin@a.gov"));

        var orgB = OrganisationEntity.Create(
            "Town Office",
            "town-office",
            new Address("2 Main", "Kandy", "20000", "LK"),
            OrganisationType.Government,
            new EmailAddress("admin@b.gov"));

        var users = new[]
        {
            User.Create(orgA.Id, "Staff A", new EmailAddress("member@example.com"), null, "hash", UserRole.Staff),
            User.Create(orgB.Id, "Admin B", new EmailAddress("MEMBER@example.com"), null, "hash", UserRole.OrgAdmin),
            User.Create(orgB.Id, "Consumer", new EmailAddress("member@example.com"), null, "hash", UserRole.User),
        };

        _contextMock.Setup(c => c.Users).Returns(AsyncQueryableHelper.BuildMockDbSet(users).Object);
        _contextMock.Setup(c => c.Organisations).Returns(AsyncQueryableHelper.BuildMockDbSet(new[] { orgA, orgB }).Object);

        var handler = new ResolveMemberLoginQueryHandler(_contextMock.Object);
        var result = await handler.Handle(new ResolveMemberLoginQuery(" member@example.com "), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Role).Should().OnlyContain(role =>
            role == "Staff" || role == "OrgAdmin" || role == "SystemAdmin");
        result.Should().Contain(x => x.LoginPath == "/login/o/city-council");
        result.Should().Contain(x => x.LoginPath == "/login/o/town-office");
    }

    [Fact]
    public async Task Handle_WhenNoMemberWorkspaces_ReturnsEmpty()
    {
        _contextMock.Setup(c => c.Users)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<User>()).Object);
        _contextMock.Setup(c => c.Organisations)
            .Returns(AsyncQueryableHelper.BuildMockDbSet(Array.Empty<OrganisationEntity>()).Object);

        var handler = new ResolveMemberLoginQueryHandler(_contextMock.Object);
        var result = await handler.Handle(new ResolveMemberLoginQuery("nobody@example.com"), CancellationToken.None);

        result.Should().BeEmpty();
    }
}