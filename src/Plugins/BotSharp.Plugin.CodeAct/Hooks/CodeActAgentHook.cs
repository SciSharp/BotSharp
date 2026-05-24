namespace BotSharp.Plugin.CodeAct.Hooks;

public class CodeActAgentHook : AgentHookBase
{
    private readonly CodeActSettings _codeActSettings;

    public override string SelfId => string.Empty;

    public CodeActAgentHook(IServiceProvider services, AgentSettings settings, CodeActSettings codeActSettings)
        : base(services, settings)
    {
        _codeActSettings = codeActSettings;
    }

    public override async Task<bool> OnFunctionsLoaded(List<FunctionDef> functions)
    {
        if (ShouldExposeExecuteCode() && functions.All(x => x.Name != "execute_code"))
        {
            functions.Add(CreateExecuteCodeFunction());
        }

        return await base.OnFunctionsLoaded(functions);
    }

    private bool ShouldExposeExecuteCode()
    {
        if (!_codeActSettings.Enabled || !_codeActSettings.ExposeExecuteCode)
        {
            return false;
        }

        return _codeActSettings.EnabledAgentIds.Count == 0 ||
               _codeActSettings.EnabledAgentIds.Contains(_agent.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static FunctionDef CreateExecuteCodeFunction()
    {
        var properties = JsonSerializer.Serialize(new
        {
            language = new
            {
                type = "string",
                description = "Code language. The pilot runtime supports python and text."
            },
            code = new
            {
                type = "string",
                description = "Complete code to execute in the restricted CodeAct runtime."
            },
            objective = new
            {
                type = "string",
                description = "Short explanation of the user goal this code is intended to satisfy."
            },
            read_only = new
            {
                type = "boolean",
                description = "Must remain true for the read-only CodeAct pilot."
            },
            metadata = new
            {
                type = "object",
                description = "Optional structured execution metadata."
            }
        });

        return new FunctionDef
        {
            Name = "execute_code",
            Description = "Execute read-only CodeAct code through the configured restricted runtime. Use this for multi-step local reasoning and deterministic read-only orchestration only; host tool bridge calls are default-deny unless explicitly allowed.",
            Impact = CodeActImpact.Read,
            Parameters = new FunctionParametersDef
            {
                Properties = JsonSerializer.Deserialize<JsonDocument>(properties) ?? JsonDocument.Parse("{}"),
                Required = ["language", "code"]
            }
        };
    }
}
