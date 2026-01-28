using AgentSkillsDotNet;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Functions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using System.Collections.Frozen;
using System.Linq;

namespace BotSharp.Plugin.AgentSkills.Hooks;

public class AgentSkillsIntegrationHook : AgentHookBase
{
    public override string SelfId => "471ca181-375f-b16f-7134-5f868ecd31c6";

    private readonly AgentSkillsFactory _skillFactory;
    private readonly AgentSkillsSettings _options;
    private readonly IEnumerable<AIFunction> _aiFunctions;

    public AgentSkillsIntegrationHook(IServiceProvider services, AgentSettings settings, IEnumerable<AIFunction> aiFunctions)
        : base(services, settings)
    {
        _skillFactory = services.GetRequiredService<AgentSkillsFactory>();
        _options = services.GetRequiredService<AgentSkillsSettings>();
        _aiFunctions = aiFunctions;
    }

    public override bool OnInstructionLoaded(string template, IDictionary<string, object> dict)
    {
        if (Agent.Type == AgentType.Routing || Agent.Type == AgentType.Planning)
        {
            return base.OnInstructionLoaded(template, dict);
        }

        // 获取当前 Agent 配置的 Prompt 技能
        var agentSkills = _skillFactory.GetAgentSkills(_options.ProjectSkillsDir);
        var instructions = agentSkills.GetInstructions();
        dict["skills_list"] = instructions;
        return base.OnInstructionLoaded(template, dict); 

    }

    public override bool OnFunctionsLoaded(List<FunctionDef> functions)
    { 
        // 遍历所有 ME.AI 的函数，将其转换为 BotSharp 的 FunctionDef
        foreach (var func in _aiFunctions)
        {
            var def = new FunctionDef
            {
                Name = func.Name,
                Description = func.Description,
                // 直接使用 AIFunction 自动生成的 JsonSchema
                // 注意：这里可能需要类型适配，视 BotSharp FunctionDef.Parameters 的具体类型定义而定
                // 通常 BotSharp 期望的是 JSON 字符串或对象结构
                Parameters = ConvertAdditionalPropertiesToFunctionParametersDef(func.AdditionalProperties)
            };

            // 防止重复添加
            if (!functions.Any(f => f.Name == def.Name))
            {
                functions.Add(def);
            }
        }

        return base.OnFunctionsLoaded(functions); 
    }


    /// <summary>
    /// 将 AdditionalProperties 转换为 FunctionParametersDef
    /// </summary>
    private FunctionParametersDef? ConvertAdditionalPropertiesToFunctionParametersDef(IReadOnlyDictionary<string, object?> additionalProperties)
    {
        if (additionalProperties == null || additionalProperties.Count == 0)
            return null;

        // 这里只是一个简单的示例实现，实际转换逻辑需根据你的需求调整
        var json = System.Text.Json.JsonSerializer.Serialize(additionalProperties);
        var doc = System.Text.Json.JsonDocument.Parse(json);

        return new FunctionParametersDef
        {
            Type = "object",
            Properties = doc,
            Required = new List<string>() // 你可以根据需要填充必需字段
        };
    }
}