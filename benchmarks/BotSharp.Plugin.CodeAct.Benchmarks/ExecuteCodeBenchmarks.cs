using System.Diagnostics;
using System.Text.Json;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Plugin.CodeAct.Functions;
using BotSharp.Plugin.CodeAct.Runtime;
using BotSharp.Plugin.CodeAct.Security;
using BotSharp.Plugin.CodeAct.Settings;

namespace BotSharp.Plugin.CodeAct.Benchmarks;

public static class ExecuteCodeBenchmarks
{
    private const int Iterations = 1_000;

    public static async Task RunAsync()
    {
        await MeasureAsync("execute_code.fake_runtime", ExecuteCodeAsync);
        Measure("security_policy.allow", SecurityPolicyAllow);
        Measure("security_policy.deny", SecurityPolicyDeny);
        Measure("token.issue_validate_consume", TokenLifecycle);
    }

    private static async Task MeasureAsync(string name, Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < Iterations; i++)
        {
            await action();
        }
        stopwatch.Stop();
        Print(name, stopwatch.Elapsed);
    }

    private static void Measure(string name, Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < Iterations; i++)
        {
            action();
        }
        stopwatch.Stop();
        Print(name, stopwatch.Elapsed);
    }

    private static async Task ExecuteCodeAsync()
    {
        var function = new ExecuteCodeFn(new FakeCodeActRuntime(), new CodeActSettings { ReadOnlyPilot = true });
        var message = new RoleDialogModel("function", string.Empty)
        {
            FunctionArgs = JsonSerializer.Serialize(new ExecuteCodeArgs
            {
                Language = "python",
                Code = "print('hello')",
                ReadOnly = true
            })
        };

        await function.Execute(message);
    }

    private static void SecurityPolicyAllow()
    {
        var policy = new DefaultCodeActSecurityPolicy(new CodeActSettings
        {
            Bridge = new CodeActBridgeSettings
            {
                Enabled = true,
                AllowedFunctions = [new CodeActAllowedFunction { Name = "read_tool", Impact = CodeActImpact.Read }]
            }
        });

        policy.Authorize(new() { FunctionName = "read_tool" });
    }

    private static void SecurityPolicyDeny()
    {
        var policy = new DefaultCodeActSecurityPolicy(new CodeActSettings
        {
            Bridge = new CodeActBridgeSettings { Enabled = true }
        });

        policy.Authorize(new() { FunctionName = "unknown_tool" });
    }

    private static void TokenLifecycle()
    {
        var service = new InMemoryCodeActTokenService(new CodeActSettings
        {
            Bridge = new CodeActBridgeSettings { TokenTtlSeconds = 60 }
        });
        var request = new CodeActTokenRequest
        {
            Audience = "sandbox",
            ConversationId = "conversation-1",
            UserId = "user-1",
            AgentId = "agent-1",
            FunctionName = "read_tool",
            Nonce = Guid.NewGuid().ToString()
        };

        var token = service.Issue(request);
        service.ValidateAndConsume(token.Token, request);
    }

    private static void Print(string name, TimeSpan elapsed)
    {
        var perOperation = elapsed.TotalMilliseconds / Iterations;
        Console.WriteLine($"{name}: total={elapsed.TotalMilliseconds:N2}ms iterations={Iterations} avg={perOperation:N4}ms");
    }
}
