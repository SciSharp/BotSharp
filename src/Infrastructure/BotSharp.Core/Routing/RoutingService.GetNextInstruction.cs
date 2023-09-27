using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Models;
using System.Drawing;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<FunctionCallFromLlm> GetNextInstruction(string prompt)
    {
        var responseFormat = _settings.EnableReasoning ?
            JsonSerializer.Serialize(new FunctionCallFromLlm()) :
            JsonSerializer.Serialize(new RoutingArgs
            {
                Function = "route_to_agent"
            });
        var content = $"{prompt} Response must be in JSON format {responseFormat}";

        var chatCompletion = CompletionProvider.GetChatCompletion(_services,
            provider: _settings.Provider,
            model: _settings.Model);

        var response = chatCompletion.GetChatCompletions(_routerInstance.Router, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, content)
            });

        var args = new FunctionCallFromLlm();
        try
        {
#if DEBUG
            Console.WriteLine(response.Content, Color.Gray);
#else
            _logger.LogInformation(response.Content);
#endif
            var pattern = @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}";
            response.Content = Regex.Match(response.Content, pattern).Value;
            args = JsonSerializer.Deserialize<FunctionCallFromLlm>(response.Content);

            // Sometimes it populate malformed Function in Agent name
            if (!string.IsNullOrEmpty(args.Function) && args.Function == args.AgentName)
            {
                args.Function = "route_to_agent";
                _logger.LogWarning($"Captured LLM malformed response");
            }

            // Another case of malformed response
            var agentService = _services.GetRequiredService<IAgentService>();
            var agents = await agentService.GetAgents();
            if (string.IsNullOrEmpty(args.AgentName) && agents.Select(x => x.Name).Contains(args.Function))
            {
                args.AgentName = args.Function;
                args.Function = "route_to_agent";
                _logger.LogWarning($"Captured LLM malformed response");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"{ex.Message}: {response.Content}");
            args.Function = "response_to_user";
            args.Answer = ex.Message;
            args.AgentName = _settings.RouterName;
        }

        if (args.Arguments != null)
        {
            SaveStateByArgs(args.Arguments);
        }

        args.Function = args.Function.Split('.').Last();

#if DEBUG
        Console.WriteLine($"*** Next Instruction *** {args}", Color.Green);
#else
        _logger.LogInformation($"*** Next Instruction *** {args}");
#endif

        return args;
    }
}
