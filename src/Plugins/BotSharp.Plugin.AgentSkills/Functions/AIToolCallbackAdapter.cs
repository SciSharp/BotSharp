using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Utilities;
using Microsoft.Extensions.AI;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AgentSkills.Functions;

public class AIToolCallbackAdapter : IFunctionCallback
{
    private readonly AIFunction _aiFunction;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    // 映射标识：直接透传 AIFunction 的名称
    public string Name => _aiFunction.Name;

    public AIToolCallbackAdapter(
        AIFunction aiFunction,
        IServiceProvider serviceProvider,
        JsonSerializerOptions? jsonOptions = null)
    {
        _aiFunction = aiFunction;
        _serviceProvider = serviceProvider;
        // 确保 JSON 配置对大小写不敏感，这是 LLM 参数传递的常见需求
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        Dictionary<string, object>? argsDictionary = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(message.FunctionArgs))
        {
            try
            {
                argsDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(
                message.FunctionArgs,
                _jsonOptions);
            }
            catch (JsonException ex)
            {
                message.Content = $"Error: Invalid JSON arguments. {ex.Message}";
                return false;
            }
        }

       

        var aiArgs = new AIFunctionArguments(argsDictionary)
        {
            Services = _serviceProvider
        };

        try
        {
            var result = await _aiFunction.InvokeAsync(aiArgs);
            message.Content = result.ConvertToString();
            return true;
        }
        catch (Exception ex)
        {
            message.Content = $"Error executing tool {Name}: {ex.Message}";
            return false;
        }
    }
 
}
