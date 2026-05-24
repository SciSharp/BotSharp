namespace BotSharp.Plugin.CodeAct.OpenSandbox;

public class OpenSandboxSession
{
    public string Id { get; set; } = string.Empty;
    public string? ContextId { get; set; }
    public Uri? DataPlaneBaseUrl { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

public class OpenSandboxSessionOptions
{
    public string Language { get; set; } = "python";
    public int TtlSeconds { get; set; } = 300;
    public int? CpuLimit { get; set; }
    public int? MemoryMb { get; set; }
    public bool EnableNetwork { get; set; }
    public List<string> AllowedHosts { get; set; } = [];
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

public class OpenSandboxRunCodeRequest
{
    public OpenSandboxSession Session { get; set; } = new();
    public string Language { get; set; } = "python";
    public string Code { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

public class OpenSandboxCodeEvent
{
    public string Type { get; set; } = OpenSandboxCodeEventTypes.Unknown;
    public string? Content { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
}

public static class OpenSandboxCodeEventTypes
{
    public const string Stdout = "stdout";
    public const string Stderr = "stderr";
    public const string Error = "error";
    public const string Completed = "completed";
    public const string Unknown = "unknown";
}
