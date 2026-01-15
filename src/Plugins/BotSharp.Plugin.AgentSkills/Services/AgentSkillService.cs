using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using BotSharp.Plugin.AgentSkills.Models;
using BotSharp.Plugin.AgentSkills.Settings;

namespace BotSharp.Plugin.AgentSkills.Services;

public class AgentSkillService : IAgentSkillService, IDisposable
{
    private readonly AgentSkillsSettings _settings;
    private readonly ILogger<AgentSkillService> _logger;
    private readonly ConcurrentDictionary<string, AgentSkill> _skills = new();
    private readonly FileSystemWatcher _watcher;
    private readonly IDeserializer _yamlDeserializer;

    public AgentSkillService(AgentSkillsSettings settings, ILogger<AgentSkillService> logger)
    {
        _settings = settings;
        _logger = logger;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        // 初始扫描
        RefreshSkills().Wait();

        // 配置 FileSystemWatcher
        var skillDir = Path.IsPathRooted(_settings.DataDir) 
            ? _settings.DataDir 
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.DataDir);

        if (Directory.Exists(skillDir))
        {
            _watcher = new FileSystemWatcher(skillDir);
            _watcher.Filter = "SKILL.md";
            _watcher.IncludeSubdirectories = true;
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            _watcher.Changed += OnSkillChanged;
            _watcher.Created += OnSkillChanged;
            _watcher.Deleted += OnSkillChanged;
            _watcher.EnableRaisingEvents = true;
        }
    }

    private void OnSkillChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation($"Detected change in skills: {e.FullPath}. Refreshing...");
        // 简单暴力：重新扫描。优化点：只更新变动的文件。
        RefreshSkills().Wait(); 
    }

    public async Task RefreshSkills()
    {
        var skillDir = Path.IsPathRooted(_settings.DataDir) 
            ? _settings.DataDir 
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.DataDir);

        if (!Directory.Exists(skillDir))
        {
            _logger.LogWarning($"Skills directory not found: {skillDir}");
            try 
            {
                Directory.CreateDirectory(skillDir);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Failed to create skills directory at {skillDir}");
                return;
            }
        }

        var newSkills = new Dictionary<string, AgentSkill>();
        var skillFiles = Directory.GetFiles(skillDir, "SKILL.md", SearchOption.AllDirectories);

        foreach (var file in skillFiles)
        {
            try
            {
                var skill = ParseSkill(file);
                if (skill != null)
                {
                    newSkills[skill.Name] = skill;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to parse skill at {file}");
            }
        }

        _skills.Clear();
        foreach (var kv in newSkills)
        {
            _skills[kv.Key] = kv.Value;
        }
        
        _logger.LogInformation($"Loaded {_skills.Count} skills.");
    }

    private AgentSkill ParseSkill(string filePath)
    {
        var content = File.ReadAllText(filePath);
        
        // 简单的 Frontmatter 解析：查找两个 --- 之间的内容
        // 注意：不完美，假设文件严格以 --- 开头
        if (!content.StartsWith("---")) return null;

        var endYaml = content.IndexOf("---", 3);
        if (endYaml == -1) return null;

        var yaml = content.Substring(3, endYaml - 3);
        var markdown = content.Substring(endYaml + 3).Trim();

        var frontmatter = _yamlDeserializer.Deserialize<SkillFrontmatter>(yaml);
        if (string.IsNullOrWhiteSpace(frontmatter.Name)) return null;

        var baseDir = Path.GetDirectoryName(filePath);
        var scriptDir = Path.Combine(baseDir, "scripts");
        var scripts = Directory.Exists(scriptDir) 
            ? Directory.GetFiles(scriptDir).Select(Path.GetFileName).ToList() 
            : new List<string>();

        var resourceDir = Path.Combine(baseDir, "resources");
        var resources = Directory.Exists(resourceDir) 
            ? Directory.GetFiles(resourceDir).Select(Path.GetFileName).ToList() 
            : new List<string>();

        return new AgentSkill
        {
            Name = frontmatter.Name,
            Description = frontmatter.Description,
            MarkdownBody = markdown,
            BaseDir = baseDir,
            Scripts = scripts,
            Resources = resources
        };
    }

    public Task<List<AgentSkill>> GetAvailableSkills()
    {
        return Task.FromResult(_skills.Values.ToList());
    }

    public Task<AgentSkill> GetSkill(string name)
    {
        if (_skills.TryGetValue(name, out var skill))
        {
            return Task.FromResult(skill);
        }
        return Task.FromResult<AgentSkill>(null);
    }

    public string GetScriptPath(string skillName, string scriptFile)
    {
        if (_skills.TryGetValue(skillName, out var skill))
        {
            // 安全检查：防止路径遍历
            if (scriptFile.Contains("..") || Path.IsPathRooted(scriptFile))
                throw new ArgumentException("Invalid script path");

            var path = Path.Combine(skill.BaseDir, "scripts", scriptFile);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    public void Dispose()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
    }
}
