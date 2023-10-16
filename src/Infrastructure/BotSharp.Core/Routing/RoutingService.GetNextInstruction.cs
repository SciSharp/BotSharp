using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<FunctionCallFromLlm> GetNextInstruction()
    {
        var content = GetNextStepPrompt();

        RoleDialogModel response = default;
        var args = new FunctionCallFromLlm();

        if (_settings.UseTextCompletion)
        {
            var completion = CompletionProvider.GetTextCompletion(_services,
                provider: _settings.Provider,
                model: _settings.Model);

            content = _routerInstance.Router.Instruction + "\r\n\r\n" + content + "\r\nResponse: ";

            int retryCount = 0;

            while (retryCount < 3)
            {
                try
                {
                    var text = await completion.GetCompletion(content);
                    response = new RoleDialogModel(AgentRole.Assistant, text);

                    var pattern = @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}";
                    response.Content = Regex.Match(response.Content, pattern).Value;
                    args = JsonSerializer.Deserialize<FunctionCallFromLlm>(response.Content);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{ex.Message}: {response.Content}");
                    args.Function = "response_to_user";
                    args.Response = ex.Message;
                    args.AgentName = "Router";
                    content += "\r\nPlease response in JSON format.";
                }
                finally
                {
                    retryCount++;
                }
            }
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
                    args.Response = ex.Message;
                    args.AgentName = "Router";
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

#if !DEBUG
    [MemoryCache(60 * 60)]
#endif
    private string GetNextStepPrompt()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        // _routerInstance.Router.Templates.First(x => x.Name == "next_step_prompt").Content;
        var template = db.GetAgentTemplate(_routerInstance.AgentId, "next_step_prompt");

        // If enabled reasoning
        // JsonSerializer.Serialize(new FunctionCallFromLlm());

        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { "enabled_reasoning", _settings.EnableReasoning }
        });
    }
}
