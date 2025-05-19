using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Helpers;

internal static class AiFunctionHelper
{
    public static FunctionDef? MapToFunctionDef(McpClientTool tool)
    {
        if (tool == null)
        {
            return null;
        }

        var properties = "{}";
        var required = "[]";

        if (tool.JsonSchema.TryGetProperty("properties", out var p))
        {
            properties = p.GetRawText();
        }

        if (tool.JsonSchema.TryGetProperty("required", out var r))
        {
            required = r.GetRawText();
        }

        var funDef = new FunctionDef
        {
            Name = tool.Name,
            Description = tool.Description ?? string.Empty,
            Type = "function",
            Parameters = new FunctionParametersDef
            {
                Type = "object",
                Properties = JsonDocument.Parse(properties),
                Required = JsonSerializer.Deserialize<List<string>>(required) ?? []
            }
        };

        return funDef;
    }
}
