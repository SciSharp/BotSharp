using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Planning;
using System.IO;

namespace BotSharp.Core.Routing.Planning;

public partial class TwoStagePlanner : IPlaner
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    public int MaxLoopCount => 100;
    private bool _isTaskCompleted;
    private string _md5;

    private Queue<FirstStagePlan> _plan1st = new Queue<FirstStagePlan>();
    private Queue<SecondStagePlan> _plan2nd = new Queue<SecondStagePlan>();

    private List<string> _executionContext = new List<string>();

    public TwoStagePlanner(IServiceProvider services, ILogger<TwoStagePlanner> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "botsharp", "cache");
        if (_plan1st.IsNullOrEmpty() && _plan2nd.IsNullOrEmpty())
        {
            Directory.CreateDirectory(tempDir);
            _md5 = Utilities.HashText(string.Join(".", dialogs.Where(x => x.Role == AgentRole.User)), "botsharp");
            var filePath = Path.Combine(tempDir, $"{_md5}-1st.json");
            FirstStagePlan[] items = new FirstStagePlan[0];
            if (File.Exists(filePath))
            {
                var cache = File.ReadAllText(filePath);
                items = JsonSerializer.Deserialize<FirstStagePlan[]>(cache);
            }
            else
            {
                items = await GetFirstStagePlanAsync(router, messageId, dialogs);

                var cache = JsonSerializer.Serialize(items);
                File.WriteAllText(filePath, cache);
            }

            foreach (var item in items)
            {
                _plan1st.Enqueue(item);
            };
        }

        // Get Second Stage Plan
        if (_plan2nd.IsNullOrEmpty())
        {
            var plan1 = _plan1st.Dequeue();

            if (plan1.ContainMultipleSteps)
            {
                var filePath = Path.Combine(tempDir, $"{_md5}-2nd-{plan1.Step}.json");
                SecondStagePlan[] items = new SecondStagePlan[0];
                if (File.Exists(filePath))
                {
                    var cache = File.ReadAllText(filePath);
                    items = JsonSerializer.Deserialize<SecondStagePlan[]>(cache);
                }
                else
                {
                    items = await GetSecondStagePlanAsync(router, messageId, plan1, dialogs);

                    var cache = JsonSerializer.Serialize(items);
                    File.WriteAllText(filePath, cache);
                }

                foreach (var item in items)
                {
                    _plan2nd.Enqueue(item);
                }
            }
            else
            {
                _plan2nd.Enqueue(new SecondStagePlan
                {
                    Description = plan1.Task,
                    Tables = plan1.Tables,
                    Parameters = plan1.Parameters,
                    Results = plan1.Results,
                });
            }
        }

        var plan2 = _plan2nd.Dequeue();

        var secondStagePrompt = GetSecondStageTaskPrompt(router, plan2);
        var inst = new FunctionCallFromLlm
        {
            AgentName = "SQL Driver",
            Response = secondStagePrompt,
            Function = "route_to_agent"
        };

        inst.HandleDialogsByPlanner = true;
        _isTaskCompleted = _plan1st.IsNullOrEmpty() && _plan2nd.IsNullOrEmpty();

        return inst;
    }

    public List<RoleDialogModel> BeforeHandleContext(FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var question = inst.Response;
        if (_executionContext.Count > 0)
        {
            var content = GetContext();
            question = $"CONTEXT:\r\n{content}\r\n" + inst.Response;
        }
        else
        {
            question = $"CONTEXT:\r\n{question}";
        }

        var taskAgentDialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, question)
            {
                MessageId = message.MessageId,
            }
        };

        return taskAgentDialogs;
    }

    public bool AfterHandleContext(List<RoleDialogModel> dialogs, List<RoleDialogModel> taskAgentDialogs)
    {
        dialogs.AddRange(taskAgentDialogs.Skip(1));

        // Keep execution context
        _executionContext.Add(taskAgentDialogs.Last().Content);

        return true;
    }

    public async Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        dialogs.Add(new RoleDialogModel(AgentRole.User, inst.Response)
        {
            MessageId = message.MessageId,
            CurrentAgentId = router.Id
        });
        return true;
    }

    public async Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        var context = _services.GetRequiredService<IRoutingContext>();

        if (message.StopCompletion || _isTaskCompleted)
        {
            context.Empty();
            return false;
        }

        var routing = _services.GetRequiredService<IRoutingService>();
        routing.ResetRecursiveCounter();
        return true;
    }
}
