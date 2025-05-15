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

        if (!tool.JsonSchema.TryGetProperty("properties", out var properties))
        {
            properties = JsonDocument.Parse("{}").RootElement;
        }

        if (!tool.JsonSchema.TryGetProperty("required", out var required))
        {
            required = JsonDocument.Parse("[]").RootElement;
        }

        var funDef = new FunctionDef
        {
            Name = tool.Name,
            Description = tool.Description ?? string.Empty,
            Type = "function",
            Parameters = new FunctionParametersDef
            {
                Type = "object",
                Properties = JsonDocument.Parse(properties.GetRawText() ?? "{}"),
                Required = JsonSerializer.Deserialize<List<string>>(required.GetRawText() ?? "[]") ?? []
            }
        };

        return funDef;
    }
}
