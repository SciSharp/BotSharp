using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Utilities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.AgentSkills.Functions;

/// <summary>
/// AIFunction to IFunctionCallback adapter.
/// Adapts Microsoft.Extensions.AI AIFunction to BotSharp's IFunctionCallback interface.
/// Implements requirements: FR-4.1, FR-4.2, FR-4.3, NFR-2.2
/// </summary>
public class AIToolCallbackAdapter : IFunctionCallback
{
    private readonly AIFunction _aiFunction;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIToolCallbackAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Gets the name of the tool.
    /// Implements requirement: FR-4.1 (Tool name mapping)
    /// </summary>
    public string Name => _aiFunction.Name;

    /// <summary>
    /// Gets the provider name for this tool.
    /// Implements requirement: FR-4.1
    /// </summary>
    public string Provider => "AgentSkills";

    /// <summary>
    /// Initializes a new instance of the AIToolCallbackAdapter class.
    /// Implements requirement: FR-4.1
    /// </summary>
    /// <param name="aiFunction">The AIFunction to adapt.</param>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="logger">Optional logger for recording operations.</param>
    /// <param name="jsonOptions">Optional JSON serialization options.</param>
    public AIToolCallbackAdapter(
        AIFunction aiFunction,
        IServiceProvider serviceProvider,
        ILogger<AIToolCallbackAdapter>? logger = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        _aiFunction = aiFunction ?? throw new ArgumentNullException(nameof(aiFunction));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? serviceProvider.GetService<ILogger<AIToolCallbackAdapter>>() 
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AIToolCallbackAdapter>.Instance;
        
        // FR-4.2: Configure JSON parsing options (case-insensitive)
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Executes the tool function.
    /// Implements requirements: FR-4.1, FR-4.2, FR-4.3, NFR-2.2
    /// </summary>
    /// <param name="message">The message containing function arguments and receiving the result.</param>
    /// <returns>True if execution succeeded, false otherwise.</returns>
    public async Task<bool> Execute(RoleDialogModel message)
    {
        // NFR-2.2: Record tool invocation
        _logger.LogDebug("Executing tool {ToolName} with args: {Args}", 
            Name, message.FunctionArgs);

        // FR-4.2: Parse arguments
        Dictionary<string, object>? argsDictionary = null;
        if (!string.IsNullOrWhiteSpace(message.FunctionArgs))
        {
            try
            {
                argsDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    message.FunctionArgs,
                    _jsonOptions);
                
                _logger.LogDebug("Parsed {Count} arguments for tool {ToolName}", 
                    argsDictionary?.Count ?? 0, Name);
            }
            catch (JsonException ex)
            {
                // FR-4.3: Argument parsing failure
                var errorMsg = $"Error: Invalid JSON arguments. {ex.Message}";
                message.Content = errorMsg;
                _logger.LogWarning(ex, "Failed to parse arguments for tool {ToolName}", Name);
                return false;
            }
        }

        // FR-4.1: Call AIFunction
        var aiArgs = new AIFunctionArguments(argsDictionary ?? new Dictionary<string, object>())
        {
            Services = _serviceProvider
        };

        try
        {
            // Execute tool
            var result = await _aiFunction.InvokeAsync(aiArgs);
            message.Content = result?.ConvertToString() ?? string.Empty;
            
            // NFR-2.2: Record successful execution
            _logger.LogInformation("Tool {ToolName} executed successfully, result length: {Length}", 
                Name, message.Content?.Length ?? 0);
            
            return true;
        }
        catch (FileNotFoundException ex)
        {
            // FR-4.3: File not found
            var errorMsg = $"Skill or file not found: {ex.Message}";
            message.Content = errorMsg;
            _logger.LogWarning(ex, "File not found when executing tool {ToolName}", Name);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            // FR-4.3, FR-5.1: Access denied (path security violation)
            var errorMsg = $"Access denied: {ex.Message}";
            message.Content = errorMsg;
            _logger.LogError(ex, "Unauthorized access attempt in tool {ToolName}", Name);
            return false;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("size", StringComparison.OrdinalIgnoreCase))
        {
            // FR-4.3, FR-5.2: File size exceeds limit
            var errorMsg = $"File size exceeds limit: {ex.Message}";
            message.Content = errorMsg;
            _logger.LogWarning(ex, "File size limit exceeded in tool {ToolName}", Name);
            return false;
        }
        catch (Exception ex)
        {
            // FR-4.3: Other errors
            var errorMsg = $"Error executing tool {Name}: {ex.Message}";
            message.Content = errorMsg;
            _logger.LogError(ex, "Unexpected error executing tool {ToolName}", Name);
            return false;
        }
    }
}
