using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace BotSharp.Abstraction.Agents;

public abstract class AgentHookBase : IAgentHook
{
    public virtual string SelfId => throw new NotImplementedException("Please set SelfId as agent id!");

    protected Agent _agent;
    public Agent Agent => _agent;
    
    protected readonly IServiceProvider _services;
    protected readonly AgentSettings _settings;

    public AgentHookBase(IServiceProvider services, AgentSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public void SetAget(Agent agent)
    {
        _agent = agent;
    }

    public virtual bool OnAgentLoading(ref string id)
    {
        return true;
    }

    public virtual bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        dict["current_date"] = $"{DateTime.Now:MMM dd, yyyy}";
        dict["current_time"] = $"{DateTime.Now:hh:mm tt}";
        dict["current_weekday"] = $"{DateTime.Now:dddd}";
        return true;
    }

    public virtual bool OnFunctionsLoaded(List<FunctionDef> functions)
    {
        _agent.Functions = functions;
        return true;
    }

    public virtual bool OnSamplesLoaded(List<string> samples)
    {
        _agent.Samples = samples;
        return true;
    }

    public virtual void  OnAgentLoaded(Agent agent)
    {
    }

    public virtual void OnAgentUtilityloaded(Agent agent)
    {
        if (agent.Type == AgentType.Routing) return;

        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        if (!isConvMode) return;

        agent.Functions ??= [];
        agent.Utilities ??= [];

        var (functions, templates) = GetUtilityContent(agent);

        agent.Functions.AddRange(functions);
        foreach (var prompt in templates)
        {
            agent.Instruction += $"\r\n\r\n{prompt}\r\n\r\n";
        }
    }

    private (IEnumerable<FunctionDef>, IEnumerable<string>) GetUtilityContent(Agent agent)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var (functionNames, templateNames) = GetUniqueContent(agent.Utilities);

        if (agent.MergeUtility)
        {
            var routing = _services.GetRequiredService<IRoutingContext>();
            var entryAgentId = routing.EntryAgentId;
            if (!string.IsNullOrEmpty(entryAgentId))
            {
                var entryAgent = db.GetAgent(entryAgentId);
                var (fns, tps) = GetUniqueContent(entryAgent?.Utilities);
                functionNames = functionNames.Concat(fns).Distinct().ToList();
                templateNames = templateNames.Concat(tps).Distinct().ToList();
            }
        }

        var ua = db.GetAgent(BuiltInAgentId.UtilityAssistant);
        var functions = ua?.Functions?.Where(x => functionNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase))?.ToList() ?? [];
        var templates = ua?.Templates?.Where(x => templateNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase))?.Select(x => x.Content)?.ToList() ?? [];
        return (functions, templates);
    }

    private (IEnumerable<string>, IEnumerable<string>) GetUniqueContent(IEnumerable<AgentUtility>? utilities)
    {
        if (utilities.IsNullOrEmpty())
        {
            return ([], []);
        }

        utilities = utilities?.Where(x => !string.IsNullOrEmpty(x.Name) && !x.Disabled)?.ToList() ?? [];
        var functionNames = utilities.SelectMany(x => x.Functions)
                                     .Where(x => !string.IsNullOrEmpty(x.Name))
                                     .Select(x => x.Name)
                                     .Distinct().ToList();
        var templateNames = utilities.SelectMany(x => x.Templates)
                                     .Where(x => !string.IsNullOrEmpty(x.Name))
                                     .Select(x => x.Name)
                                     .Distinct().ToList();

        return (functionNames, templateNames);
    }
}
