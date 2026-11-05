using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Users;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Abstraction.Users.Settings;
using BotSharp.Core.Users.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BotSharp.Core.UnitTests.Users;

/// <summary>
/// CreateUser merges a new user into an unverified record with the same username, phone or email. A host
/// platform whose emails are not unique (many accounts share a placeholder email) had each new user merged
/// into someone else's record, without its ExternalId, so the user was "created" again on every message.
/// CreateExternalUser keys only on ExternalId.
/// </summary>
public class CreateExternalUserTests
{
    private const string ExternalId = "877992";

    private readonly Mock<IBotSharpRepository> _db = new();
    private readonly Mock<IAuthenticationHook> _hook = new();

    private UserService BuildService()
    {
        var services = new ServiceCollection()
            .AddSingleton(_db.Object)
            .AddSingleton(_hook.Object)
            .BuildServiceProvider();
        return new UserService(services, Mock.Of<IUserIdentity>(), NullLogger<UserService>.Instance, new AccountSetting());
    }

    [Fact]
    public async Task Inserts_a_record_keyed_on_ExternalId_even_when_another_user_has_the_same_email()
    {
        var other = new User { Id = Guid.NewGuid().ToString(), UserName = "someone-else", Email = "no@no.no", ExternalId = "224733" };
        _db.Setup(x => x.GetUserByEmail("no@no.no")).ReturnsAsync(other);
        _db.Setup(x => x.GetUserByUserName(It.IsAny<string>())).ReturnsAsync((User?)null);
        _db.Setup(x => x.GetUserById(ExternalId)).ReturnsAsync((User?)null);
        User? inserted = null;
        _db.Setup(x => x.CreateUser(It.IsAny<User>())).Callback<User>(u => inserted = u).Returns(Task.CompletedTask);

        var created = await BuildService().CreateExternalUser(new User
        {
            UserName = "IH001T21",
            Email = "No@No.No",
            Password = string.Empty,
            ExternalId = ExternalId,
        });

        Assert.Same(inserted, created);
        Assert.NotEqual(other.Id, created.Id);
        Assert.False(string.IsNullOrEmpty(created.Id));
        Assert.Equal(ExternalId, created.ExternalId);
        Assert.Equal("ih001t21", created.UserName);
        Assert.Equal("no@no.no", created.Email);
        Assert.False(string.IsNullOrEmpty(created.Salt));
        _db.Verify(x => x.UpdateExistUser(It.IsAny<string>(), It.IsAny<User>()), Times.Never);
        _hook.Verify(x => x.UserCreated(created), Times.Once);
    }

    [Fact]
    public async Task Returns_the_record_already_bound_to_the_ExternalId_without_inserting()
    {
        var existing = new User { Id = Guid.NewGuid().ToString(), UserName = "ih001t21", ExternalId = ExternalId };
        _db.Setup(x => x.GetUserById(ExternalId)).ReturnsAsync(existing);

        var result = await BuildService().CreateExternalUser(new User { UserName = "ih001t21", ExternalId = ExternalId });

        Assert.Same(existing, result);
        _db.Verify(x => x.CreateUser(It.IsAny<User>()), Times.Never);
        _hook.Verify(x => x.UserCreated(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Requires_an_ExternalId()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => BuildService().CreateExternalUser(new User { UserName = "ih001t21" }));
        _db.Verify(x => x.CreateUser(It.IsAny<User>()), Times.Never);
    }
}
