using McpDotNet.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotSharp.Core.MCP;

internal static class AIFunctionUtilities
{
    public static FunctionDef MapToFunctionDef(Tool tool)
    {
        if (tool == null)
        {
            throw new ArgumentNullException(nameof(tool));
        }

        var properties = tool.InputSchema?.Properties;
        var required = tool.InputSchema?.Required ?? new List<string>();

        FunctionDef funDef = new FunctionDef
        {
            Name = tool.Name,
            Description = tool.Description?? string.Empty,
            Type = "function",
            Parameters = new FunctionParametersDef
            {
                Type = "object",
                Properties = properties != null ? JsonSerializer.SerializeToDocument(properties) : JsonDocument.Parse("{}"),
                Required = required
            }
        };

        return funDef;
    }
}
