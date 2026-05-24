namespace BotSharp.Plugin.CodeAct.UnitTests.Hooks;

public class CodeActAgentHookTests
{
    [Fact]
    public async Task OnFunctionsLoaded_Adds_ExecuteCode_WhenEnabled()
    {
        var hook = CreateHook(new CodeActSettings { Enabled = true, ExposeExecuteCode = true });
        var agent = new Agent { Id = "agent-1" };
        hook.SetAgent(agent);
        var functions = new List<FunctionDef>();

        await hook.OnFunctionsLoaded(functions);

        var function = Assert.Single(agent.Functions);
        Assert.Equal("execute_code", function.Name);
        Assert.Equal(CodeActImpact.Read, function.Impact);
        Assert.NotNull(function.Parameters);
        Assert.Contains("code", function.Parameters.Required);
    }

    [Fact]
    public async Task OnFunctionsLoaded_DoesNotDuplicate_ExecuteCode()
    {
        var hook = CreateHook(new CodeActSettings { Enabled = true, ExposeExecuteCode = true });
        var agent = new Agent { Id = "agent-1" };
        hook.SetAgent(agent);
        var functions = new List<FunctionDef> { new() { Name = "execute_code" } };

        await hook.OnFunctionsLoaded(functions);

        Assert.Single(agent.Functions, x => x.Name == "execute_code");
    }

    [Fact]
    public async Task OnFunctionsLoaded_DoesNotExpose_WhenDisabled()
    {
        var hook = CreateHook(new CodeActSettings { Enabled = false, ExposeExecuteCode = true });
        var agent = new Agent { Id = "agent-1" };
        hook.SetAgent(agent);
        var functions = new List<FunctionDef>();

        await hook.OnFunctionsLoaded(functions);

        Assert.Empty(agent.Functions);
    }

    private static CodeActAgentHook CreateHook(CodeActSettings settings)
    {
        return new CodeActAgentHook(new ServiceCollection().BuildServiceProvider(), new AgentSettings(), settings);
    }
}
