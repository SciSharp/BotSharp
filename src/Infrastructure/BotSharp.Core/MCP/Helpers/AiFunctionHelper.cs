using System.Text.Json;
using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Helpers;

internal static class AiFunctionHelper
{
    public static FunctionDef MapToFunctionDef(McpClientTool tool)
    {
        if (tool == null)
        {
            throw new ArgumentNullException(nameof(tool));
        }

        var properties = tool.JsonSchema.GetProperty("properties");
        var required = tool.JsonSchema.GetProperty("required");

        var funDef = new FunctionDef
        {
            Name = tool.Name,
            Description = tool.Description ?? string.Empty,
            Type = "function",
            Parameters = new FunctionParametersDef
            {
                Type = "object",
                Properties = JsonDocument.Parse(properties.GetRawText()),
                Required = JsonSerializer.Deserialize<List<string>>(required.GetRawText())
            }
        };

        return funDef;
    }
}
