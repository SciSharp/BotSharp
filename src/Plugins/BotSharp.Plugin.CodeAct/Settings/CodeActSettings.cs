namespace BotSharp.Plugin.CodeAct.Settings;

public class CodeActSettings
{
    public bool Enabled { get; set; }
    public bool ExposeExecuteCode { get; set; }
    public string Runtime { get; set; } = "fake";
    public bool ReadOnlyPilot { get; set; } = true;
    public int ExecutionTimeoutSeconds { get; set; } = 10;
    public List<string> EnabledAgentIds { get; set; } = [];
    public CodeActBridgeSettings Bridge { get; set; } = new();
    public OpenSandboxCodeActSettings OpenSandbox { get; set; } = new();
}

public class CodeActBridgeSettings
{
    public bool Enabled { get; set; }
    public int TokenTtlSeconds { get; set; } = 60;
    public List<CodeActAllowedFunction> AllowedFunctions { get; set; } = [];
}

public class CodeActAllowedFunction
{
    public string Name { get; set; } = string.Empty;
    public string Impact { get; set; } = CodeActImpact.Read;
    public bool RequiresApproval { get; set; }
}

public class OpenSandboxCodeActSettings
{
    public string ControlPlaneBaseUrl { get; set; } = string.Empty;
    public string DataPlaneBaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string RuntimeImage { get; set; } = "opensandbox/code-interpreter:v1.0.2";
    public List<string> Entrypoint { get; set; } = ["/opt/opensandbox/code-interpreter.sh"];
    public string CreateSandboxPath { get; set; } = "/sandboxes";
    public string DestroySandboxPath { get; set; } = "/sandboxes/{sandboxId}";
    public string CreateContextPath { get; set; } = "/code/contexts";
    public string RunCodePath { get; set; } = "/code";
    public string Language { get; set; } = "python";
    public bool CreateSandboxPerExecution { get; set; } = true;
    public string SandboxId { get; set; } = string.Empty;
    public int SandboxTtlSeconds { get; set; } = 300;
    public int MaxStdoutChars { get; set; } = 20000;
    public int MaxStderrChars { get; set; } = 12000;
    public int MaxTraceEvents { get; set; } = 200;
    public int? CpuLimit { get; set; }
    public int? MemoryMb { get; set; }
    public bool EnableNetwork { get; set; }
    public List<string> AllowedHosts { get; set; } = [];
}
