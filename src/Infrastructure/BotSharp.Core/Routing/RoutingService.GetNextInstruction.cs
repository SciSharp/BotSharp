using BotSharp.Abstraction.Functions.Models;
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

        var state = _services.GetRequiredService<IConversationStateService>();

        RoleDialogModel response = default;
        var args = new FunctionCallFromLlm();

        if (_settings.UseTextCompletion)
        {
            var completion = CompletionProvider.GetTextCompletion(_services,
                provider: _settings.Provider,
                model: _settings.Model);

            content = _routerInstance.Router.Instruction + "\r\n\r\n" + content + "\r\nResponse: ";
            var text = await completion.GetCompletion(content);
            response = new RoleDialogModel(AgentRole.Assistant, text);
        }
        else
        {
            var completion = CompletionProvider.GetChatCompletion(_services,
                provider: _settings.Provider,
                model: _settings.Model);

            int retryCount = 0;

            while (retryCount < 3)
            {
                try
                {
                    response = completion.GetChatCompletions(_routerInstance.Router, new List<RoleDialogModel>
                    {
                        new RoleDialogModel(AgentRole.User, content)
                    });

                    var pattern = @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}";
                    response.Content = Regex.Match(response.Content, pattern).Value;
                    args = JsonSerializer.Deserialize<FunctionCallFromLlm>(response.Content);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex.Message}: {response.Content}");
                    args.Function = "response_to_user";
                    args.Answer = ex.Message;
                    args.AgentName = _settings.RouterName;
                    content += "\r\nPlease response in JSON format.";
                }
                finally 
                { 
                    retryCount++; 
                }
            }
        }

#if DEBUG
        Console.WriteLine(response.Content, Color.Gray);
#else
        _logger.LogInformation(response.Content);
#endif

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
