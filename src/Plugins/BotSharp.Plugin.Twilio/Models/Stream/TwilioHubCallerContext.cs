using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading;

namespace BotSharp.Plugin.Twilio.Models.Stream;

public class TwilioHubCallerContext : HubCallerContext
{
    private readonly HubConnectionContext _connection;

    public TwilioHubCallerContext(HubConnectionContext connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public override string ConnectionId => _connection.ConnectionId;

    /// <inheritdoc />
    public override string? UserIdentifier => _connection.UserIdentifier;

    /// <inheritdoc />
    public override ClaimsPrincipal? User => _connection.User;

    /// <inheritdoc />
    public override IDictionary<object, object?> Items => _connection.Items;

    /// <inheritdoc />
    public override IFeatureCollection Features => _connection.Features;

    /// <inheritdoc />
    public override CancellationToken ConnectionAborted => _connection.ConnectionAborted;

    /// <inheritdoc />
    public override void Abort() => _connection.Abort();
}
