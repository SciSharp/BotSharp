using BotSharp.Abstraction.Functions.Models;
using ModelContextProtocol.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace BotSharp.Core.MCP;

internal static class AIFunctionUtilities
{
    public static FunctionDef MapToFunctionDef(Tool tool)
    {
        if (tool == null)
        {
            throw new ArgumentNullException(nameof(tool));
        }

        var properties = tool.InputSchema.GetProperty("properties");
        var required = tool.InputSchema.GetProperty("required");

        FunctionDef funDef = new FunctionDef
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
