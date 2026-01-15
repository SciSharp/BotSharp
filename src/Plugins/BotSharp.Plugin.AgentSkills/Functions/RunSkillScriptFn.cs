using System.Threading.Tasks;
using System.Text.Json;
using System;
using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Plugin.AgentSkills.Services;
using BotSharp.Plugin.PythonInterpreter.Interfaces;

namespace BotSharp.Plugin.AgentSkills.Functions;

public class RunSkillScriptFn : IFunctionCallback
{
    public string Name => "run_skill_script";
    public string Indication => "Running skill script...";
    private readonly IServiceProvider _services;

    public RunSkillScriptFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(message.FunctionArgs);
        var skillName = args.TryGetProperty("skill_name", out var t1) ? t1.GetString() : null;
        var scriptFile = args.TryGetProperty("script_file", out var t2) ? t2.GetString() : null;
        // 简单处理 args，假设直接是字符串形式的命令行参数，或者 JSON 字符串
        // 如果是 JSON 对象，需要转换。为了简单起见，这里假设 LLM 传入的是参数字符串
        var scriptArgs = args.TryGetProperty("args", out var t3) ? t3.GetString() : "";

        if (string.IsNullOrEmpty(skillName) || string.IsNullOrEmpty(scriptFile))
        {
            message.Content = "Error: skill_name and script_file are required.";
            return false;
        }

        var skillService = _services.GetRequiredService<IAgentSkillService>();
        string scriptPath;
        try
        {
            scriptPath = skillService.GetScriptPath(skillName, scriptFile);
            if (string.IsNullOrEmpty(scriptPath))
            {
                 message.Content = $"Error: Script '{scriptFile}' not found in skill '{skillName}'.";
                 return false;
            }
        }
        catch (Exception ex)
        {
            message.Content = $"Error: {ex.Message}";
            return false;
        }

        // 目前仅支持 .py
        if (scriptPath.EndsWith(".py"))
        {
            var runner = _services.GetRequiredService<IPyScriptRunner>();
            try 
            {
                var output = await runner.RunScript(scriptPath, scriptArgs);
                message.Content = output;
                return true;
            }
            catch (Exception ex)
            {
                message.Content = $"Script execution error: {ex.Message}";
                return false;
            }
        }
        else 
        {
             message.Content = "Error: Only .py scripts are supported currently.";
             return false;
        }
    }
}
