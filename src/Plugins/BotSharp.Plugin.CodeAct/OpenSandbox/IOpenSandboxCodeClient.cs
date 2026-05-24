namespace BotSharp.Plugin.CodeAct.OpenSandbox;

public interface IOpenSandboxCodeClient
{
    Task<OpenSandboxSession> CreateSessionAsync(OpenSandboxSessionOptions options, CancellationToken cancellationToken);

    IAsyncEnumerable<OpenSandboxCodeEvent> RunCodeAsync(OpenSandboxRunCodeRequest request, CancellationToken cancellationToken);

    Task DestroySessionAsync(OpenSandboxSession session, CancellationToken cancellationToken);
}
