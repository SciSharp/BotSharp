---
feature: agent-skills-refactor
created: 2026-01-28
updated: 2026-01-28
status: draft
---

# Agent Skills 插件重构设计

## 需求追溯

本设计文档实现以下需求：
- FR-1.x: 技能发现与加载
- FR-2.x: 技能元数据注入
- FR-3.x: 技能激活（渐进式披露）
- FR-4.x: 工具执行
- FR-5.x: 安全性
- FR-6.x: 配置管理
- NFR-1.x: 性能需求
- NFR-2.x: 可维护性需求
- NFR-3.x: 兼容性需求
- NFR-4.x: 可扩展性需求

## 1. 架构概述

本设计基于 **AgentSkillsDotNet** 库实现，遵循 Agent Skills 规范，提供基于工具的技能集成方式（Tool-based Integration）和渐进式披露机制。

### 1.1 核心组件

```
BotSharp.Plugin.AgentSkills
├── AgentSkillsPlugin.cs              # 插件入口，初始化 AgentSkillsFactory
├── Settings/
│   └── AgentSkillsSettings.cs        # 配置管理
├── Services/
│   ├── ISkillService.cs              # 技能服务接口（封装 AgentSkillsDotNet）
│   └── SkillService.cs               # 技能服务实现
├── Functions/
│   ├── ReadSkillFunction.cs          # read_skill 工具
│   ├── ReadSkillFileFunction.cs      # read_skill_file 工具
│   └── ListSkillDirectoryFunction.cs # list_skill_directory 工具
└── Hooks/
    ├── AgentSkillsInstructionHook.cs # 指令注入钩子
    └── AgentSkillsFunctionHook.cs    # 函数注册钩子
```

### 1.2 AgentSkillsDotNet 库集成

**核心类：**
- `AgentSkillsFactory`: 技能工厂，负责创建 AgentSkills 实例
- `AgentSkills`: 技能集合，提供技能访问和工具转换
- `AgentSkillsAsToolsStrategy`: 工具转换策略枚举
- `AgentSkillsAsToolsOptions`: 工具转换选项

**主要方法：**
- `GetAgentSkills(string? skillsDir)`: 从指定目录加载技能
- `GetAsTools(strategy, options)`: 将技能转换为 AITool 列表
- `GetInstructions()`: 获取技能指令文本

### 1.3 数据流

```
启动阶段：
1. AgentSkillsPlugin 创建 AgentSkillsFactory 实例
2. 使用 GetAgentSkills() 加载用户级和项目级技能
3. 使用 GetAsTools() 将技能转换为 AIFunction 工具
4. 注册工具到 BotSharp 的 IFunctionCallback 系统

运行阶段：
1. AgentSkillsInstructionHook 使用 GetInstructions() 注入技能列表
2. Agent 根据任务选择技能
3. Agent 调用 read_skill/read_skill_file/list_skill_directory 工具
4. 工具通过 AgentSkillsDotNet 提供的 API 访问技能内容
```

## 2. 详细设计

### 2.1 技能服务（基于 AgentSkillsDotNet）

**需求追溯**: FR-1.1, FR-1.2, FR-1.3, NFR-4.1

**职责：** 封装 AgentSkillsDotNet 库，提供统一的技能访问接口

**接口设计：**
```csharp
/// <summary>
/// 技能服务接口，封装 AgentSkillsDotNet 库功能
/// </summary>
public interface ISkillService
{
    /// <summary>
    /// 获取所有已加载的技能
    /// 实现需求: FR-1.1
    /// </summary>
    AgentSkills GetAgentSkills();
    
    /// <summary>
    /// 获取技能指令文本（用于注入到 Agent 提示）
    /// 实现需求: FR-2.1
    /// </summary>
    string GetInstructions();
    
    /// <summary>
    /// 获取技能工具列表
    /// 实现需求: FR-3.1
    /// </summary>
    IList<AITool> GetTools();
    
    /// <summary>
    /// 重新加载技能
    /// 实现需求: NFR-4.2
    /// </summary>
    Task ReloadSkillsAsync();
    
    /// <summary>
    /// 获取已加载的技能数量
    /// 实现需求: NFR-2.2 (日志记录)
    /// </summary>
    int GetSkillCount();
}
```

**实现要点：**
```csharp
public class SkillService : ISkillService
{
    private readonly AgentSkillsFactory _factory;
    private readonly AgentSkillsSettings _settings;
    private readonly ILogger<SkillService> _logger;
    private AgentSkills? _agentSkills;
    private IList<AITool>? _tools;
    private readonly object _lock = new object();
    
    public SkillService(
        AgentSkillsFactory factory, 
        AgentSkillsSettings settings,
        ILogger<SkillService> logger)
    {
        _factory = factory;
        _settings = settings;
        _logger = logger;
        InitializeSkills();
    }
    
    /// <summary>
    /// 初始化技能加载
    /// 实现需求: FR-1.1, FR-1.2, FR-1.3
    /// </summary>
    private void InitializeSkills()
    {
        lock (_lock)
        {
            try
            {
                // FR-1.2: 加载项目级技能
                if (_settings.EnableProjectSkills)
                {
                    var projectSkillsDir = _settings.GetProjectSkillsDirectory();
                    _logger.LogInformation("Loading project skills from {Directory}", projectSkillsDir);
                    
                    if (Directory.Exists(projectSkillsDir))
                    {
                        _agentSkills = _factory.GetAgentSkills(projectSkillsDir);
                        _logger.LogInformation("Loaded {Count} project skills", GetSkillCount());
                    }
                    else
                    {
                        // FR-1.3: 目录不存在时记录警告
                        _logger.LogWarning("Project skills directory not found: {Directory}", projectSkillsDir);
                    }
                }
                
                // FR-1.2: 加载用户级技能（如果需要合并多个目录）
                if (_settings.EnableUserSkills)
                {
                    var userSkillsDir = _settings.GetUserSkillsDirectory();
                    _logger.LogInformation("Loading user skills from {Directory}", userSkillsDir);
                    
                    if (Directory.Exists(userSkillsDir))
                    {
                        // 注意：AgentSkillsDotNet 可能不支持合并多个目录
                        // 如果需要，可以在这里实现合并逻辑
                        var userSkills = _factory.GetAgentSkills(userSkillsDir);
                        // TODO: 合并 userSkills 和 _agentSkills
                        _logger.LogInformation("Loaded {Count} user skills", userSkills?.Count ?? 0);
                    }
                    else
                    {
                        // FR-1.3: 目录不存在时记录警告
                        _logger.LogWarning("User skills directory not found: {Directory}", userSkillsDir);
                    }
                }
                
                // FR-3.1: 转换为工具
                if (_agentSkills != null)
                {
                    // FR-3.2: 根据配置生成工具
                    _tools = _agentSkills.GetAsTools(
                        AgentSkillsAsToolsStrategy.AvailableSkillsAndLookupTools,
                        new AgentSkillsAsToolsOptions
                        {
                            IncludeToolForFileContentRead = _settings.EnableReadFileTool,
                            // 其他选项根据 AgentSkillsDotNet 库的 API 设置
                            // MaxOutputSizeBytes = _settings.MaxOutputSizeBytes
                        }
                    );
                    
                    _logger.LogInformation("Generated {Count} tools from skills", _tools?.Count ?? 0);
                }
            }
            catch (Exception ex)
            {
                // FR-1.3: 加载失败时记录错误但不中断
                _logger.LogError(ex, "Failed to initialize skills");
                _agentSkills = null;
                _tools = new List<AITool>();
            }
        }
    }
    
    public AgentSkills GetAgentSkills() 
    {
        return _agentSkills ?? throw new InvalidOperationException("Skills not loaded");
    }
    
    public string GetInstructions() 
    {
        // FR-2.1: 使用 AgentSkillsDotNet 生成指令
        return _agentSkills?.GetInstructions() ?? string.Empty;
    }
    
    public IList<AITool> GetTools() 
    {
        return _tools ?? new List<AITool>();
    }
    
    public async Task ReloadSkillsAsync()
    {
        await Task.Run(() => InitializeSkills());
    }
    
    public int GetSkillCount()
    {
        // 假设 AgentSkills 有 Count 属性或类似方法
        return _agentSkills?.Count ?? 0;
    }
}
```

**设计决策：**
1. **单例模式**: SkillService 注册为单例，避免重复加载技能（NFR-1.1）
2. **延迟加载**: 仅在构造函数中加载元数据，完整内容按需加载（FR-3.1）
3. **错误容忍**: 单个技能加载失败不影响其他技能（FR-1.3）
4. **线程安全**: 使用锁保护技能重新加载操作（NFR-1.3）

**性能考虑：**
- 技能元数据在启动时一次性加载（NFR-1.1）
- 使用 AgentSkillsDotNet 的内置缓存机制（NFR-1.3）
- 避免重复解析 SKILL.md 文件

### 2.2 AgentSkillsDotNet 工具策略

**需求追溯**: FR-3.1, FR-3.2

**AgentSkillsAsToolsStrategy 枚举值：**
根据 AgentSkillsDotNet 库的实现，可能包括：
- `AvailableSkillsOnly`: 仅包含技能列表工具
- `AvailableSkillsAndLookupTools`: 包含技能列表和查找工具（read_skill, read_skill_file, list_skill_directory）
- 其他策略根据 AgentSkillsDotNet 库提供的选项

**AgentSkillsAsToolsOptions 配置：**
```csharp
/// <summary>
/// 工具生成选项配置
/// 实现需求: FR-3.2, FR-5.2
/// </summary>
new AgentSkillsAsToolsOptions
{
    // FR-3.2: 根据配置启用/禁用工具
    IncludeToolForFileContentRead = _settings.EnableReadFileTool,
    IncludeToolForDirectoryListing = _settings.EnableListDirectoryTool,
    
    // FR-5.2: 文件大小限制
    MaxOutputSizeBytes = _settings.MaxOutputSizeBytes,
    
    // 其他选项根据 AgentSkillsDotNet 库的 API
}
```

**生成的工具：**
当使用 `AvailableSkillsAndLookupTools` 策略时，AgentSkillsDotNet 自动生成：

1. **read_skill** (FR-3.1)
   - 描述: 读取完整 SKILL.md 内容
   - 参数: skill_name (string, required)
   - 返回: SKILL.md 的完整 Markdown 内容

2. **read_skill_file** (FR-3.1)
   - 描述: 读取技能目录中的文件
   - 参数: 
     - skill_name (string, required)
     - file_path (string, required)
   - 返回: 文件内容（文本或 Base64 编码）

3. **list_skill_directory** (FR-3.1)
   - 描述: 列出技能目录内容
   - 参数:
     - skill_name (string, required)
     - directory_path (string, optional)
   - 返回: 文件和目录列表（JSON 格式）

**安全特性：**
AgentSkillsDotNet 库内置以下安全机制（FR-5.1, FR-5.2）：
- 路径遍历防护（禁止 `../` 和 `..\`）
- 访问范围限制（仅限技能目录内）
- 文件大小限制（通过 MaxOutputSizeBytes）
- 路径规范化和验证

### 2.3 工具函数实现（使用 AgentSkillsDotNet 提供的工具）

**需求追溯**: FR-3.1, FR-4.1, FR-4.2, FR-4.3

AgentSkillsDotNet 库通过 `GetAsTools()` 方法自动生成工具，我们只需要将这些工具适配到 BotSharp 框架。

#### 2.3.1 自动生成的工具

**实现方式：**
```csharp
/// <summary>
/// 在 AgentSkillsPlugin.RegisterDI 中注册工具
/// 实现需求: FR-3.1, FR-4.1
/// </summary>
public void RegisterDI(IServiceCollection services, IConfiguration config)
{
    // ... 其他注册代码 ...
    
    // 获取技能服务
    var sp = services.BuildServiceProvider();
    var skillService = sp.GetRequiredService<ISkillService>();
    
    // FR-3.1: 获取 AgentSkillsDotNet 生成的工具
    var tools = skillService.GetTools();
    
    // FR-4.1: 将 AITool 转换为 BotSharp 的 IFunctionCallback
    foreach (var tool in tools)
    {
        if (tool is AIFunction aiFunc)
        {
            // 注册为 Scoped，每次请求创建新实例
            services.AddScoped<IFunctionCallback>(provider =>
                new AIToolCallbackAdapter(aiFunc, provider));
        }
    }
}
```

#### 2.3.2 AIToolCallbackAdapter 适配器

**需求追溯**: FR-4.1, FR-4.2, FR-4.3, NFR-2.2

**职责：** 将 Microsoft.Extensions.AI 的 AIFunction 适配为 BotSharp 的 IFunctionCallback

**完整实现：**
```csharp
/// <summary>
/// AIFunction 到 IFunctionCallback 的适配器
/// 实现需求: FR-4.1, FR-4.2, FR-4.3
/// </summary>
public class AIToolCallbackAdapter : IFunctionCallback
{
    private readonly AIFunction _aiFunction;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIToolCallbackAdapter> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // FR-4.1: 映射工具名称
    public string Name => _aiFunction.Name;
    
    public string Provider => "AgentSkills";

    public AIToolCallbackAdapter(
        AIFunction aiFunction,
        IServiceProvider serviceProvider,
        ILogger<AIToolCallbackAdapter>? logger = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        _aiFunction = aiFunction ?? throw new ArgumentNullException(nameof(aiFunction));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? serviceProvider.GetService<ILogger<AIToolCallbackAdapter>>() 
            ?? NullLogger<AIToolCallbackAdapter>.Instance;
        
        // FR-4.2: 配置 JSON 解析选项（大小写不敏感）
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// 执行工具函数
    /// 实现需求: FR-4.1, FR-4.2, FR-4.3, NFR-2.2
    /// </summary>
    public async Task<bool> Execute(RoleDialogModel message)
    {
        // NFR-2.2: 记录工具调用
        _logger.LogDebug("Executing tool {ToolName} with args: {Args}", 
            Name, message.FunctionArgs);

        // FR-4.2: 解析参数
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
                // FR-4.3: 参数解析失败
                var errorMsg = $"Error: Invalid JSON arguments. {ex.Message}";
                message.Content = errorMsg;
                _logger.LogWarning(ex, "Failed to parse arguments for tool {ToolName}", Name);
                return false;
            }
        }

        // FR-4.1: 调用 AIFunction
        var aiArgs = new AIFunctionArguments(argsDictionary ?? new Dictionary<string, object>())
        {
            Services = _serviceProvider
        };

        try
        {
            // 执行工具
            var result = await _aiFunction.InvokeAsync(aiArgs);
            message.Content = result?.ConvertToString() ?? string.Empty;
            
            // NFR-2.2: 记录成功执行
            _logger.LogInformation("Tool {ToolName} executed successfully, result length: {Length}", 
                Name, message.Content?.Length ?? 0);
            
            return true;
        }
        catch (FileNotFoundException ex)
        {
            // FR-4.3: 文件不存在
            var errorMsg = $"Skill or file not found: {ex.Message}";
            message.Content = errorMsg;
            _logger.LogWarning(ex, "File not found when executing tool {ToolName}", Name);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            // FR-4.3, FR-5.1: 访问被拒绝（路径安全违规）
            var errorMsg = $"Access denied: {ex.Message}";
            message.Content = errorMsg;
            _logger.LogError(ex, "Unauthorized access attempt in tool {ToolName}", Name);
            return false;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("size"))
        {
            // FR-4.3, FR-5.2: 文件大小超限
            var errorMsg = $"File size exceeds limit: {ex.Message}";
            message.Content = errorMsg;
            _logger.LogWarning(ex, "File size limit exceeded in tool {ToolName}", Name);
            return false;
        }
        catch (Exception ex)
        {
            // FR-4.3: 其他错误
            var errorMsg = $"Error executing tool {Name}: {ex.Message}";
            message.Content = errorMsg;
            _logger.LogError(ex, "Unexpected error executing tool {ToolName}", Name);
            return false;
        }
    }
}
```

**设计决策：**
1. **依赖注入**: 通过构造函数注入 ILogger，支持测试和日志记录（NFR-2.2）
2. **错误分类**: 区分不同类型的错误，提供友好的错误消息（FR-4.3）
3. **日志级别**: 
   - Debug: 参数解析详情
   - Info: 成功执行
   - Warning: 预期的错误（文件不存在、大小超限）
   - Error: 意外错误（NFR-2.2）
4. **线程安全**: AIFunction.InvokeAsync 是线程安全的

**优点：**
- 无需手动实现每个工具函数（NFR-2.1）
- AgentSkillsDotNet 库已处理路径安全、文件大小限制等（FR-5.1, FR-5.2）
- 自动符合 Agent Skills 规范（NFR-3.1）
- 易于测试和维护（NFR-2.3）

### 2.4 钩子实现

**需求追溯**: FR-2.1, FR-2.2, FR-3.1, NFR-2.1

#### 2.4.1 AgentSkillsInstructionHook

**需求追溯**: FR-2.1, FR-2.2

**职责：** 将技能元数据注入到 Agent 指令中

**完整实现：**
```csharp
/// <summary>
/// 技能指令注入钩子
/// 实现需求: FR-2.1, FR-2.2
/// </summary>
public class AgentSkillsInstructionHook : AgentHookBase
{
    private readonly ISkillService _skillService;
    private readonly ILogger<AgentSkillsInstructionHook> _logger;
    
    public AgentSkillsInstructionHook(
        IServiceProvider services, 
        AgentSettings settings,
        ISkillService skillService,
        ILogger<AgentSkillsInstructionHook> logger) 
        : base(services, settings)
    {
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// 指令加载时注入技能列表
    /// 实现需求: FR-2.1, FR-2.2
    /// </summary>
    public override bool OnInstructionLoaded(string template, IDictionary<string, object> dict)
    {
        // FR-2.2: 跳过 Routing 和 Planning 类型的 Agent
        if (Agent.Type == AgentType.Routing || Agent.Type == AgentType.Planning)
        {
            _logger.LogDebug("Skipping skill injection for {AgentType} agent {AgentId}", 
                Agent.Type, Agent.Id);
            return base.OnInstructionLoaded(template, dict);
        }
        
        try
        {
            // FR-2.1: 使用 AgentSkillsDotNet 提供的 GetInstructions() 方法
            var instructions = _skillService.GetInstructions();
            
            if (!string.IsNullOrEmpty(instructions))
            {
                // 注入到指令字典
                dict["available_skills"] = instructions;
                
                _logger.LogInformation(
                    "Injected {Count} skills into agent {AgentId} instructions", 
                    _skillService.GetSkillCount(), 
                    Agent.Id);
            }
            else
            {
                _logger.LogWarning("No skills available to inject for agent {AgentId}", Agent.Id);
            }
        }
        catch (Exception ex)
        {
            // 注入失败不应中断 Agent 加载
            _logger.LogError(ex, "Failed to inject skills into agent {AgentId}", Agent.Id);
        }
        
        return base.OnInstructionLoaded(template, dict);
    }
}
```

**GetInstructions() 返回格式：**
AgentSkillsDotNet 库自动生成符合规范的 XML 格式（FR-2.1）：
```xml
<available_skills>
  <skill>
    <name>pdf-processing</name>
    <description>Extracts text and tables from PDF files, fills forms, merges documents.</description>
  </skill>
  <skill>
    <name>data-analysis</name>
    <description>Analyzes datasets, generates charts, and creates summary reports.</description>
  </skill>
</available_skills>
```

**设计决策：**
1. **异常处理**: 技能注入失败不中断 Agent 加载（FR-1.3）
2. **日志记录**: 记录注入操作和技能数量（NFR-2.2）
3. **类型过滤**: 明确跳过 Routing 和 Planning 类型（FR-2.2）

#### 2.4.2 AgentSkillsFunctionHook

**需求追溯**: FR-3.1, NFR-2.1

**职责：** 注册技能工具函数到 BotSharp

**完整实现：**
```csharp
/// <summary>
/// 技能函数注册钩子
/// 实现需求: FR-3.1
/// </summary>
public class AgentSkillsFunctionHook : AgentHookBase
{
    private readonly ISkillService _skillService;
    private readonly ILogger<AgentSkillsFunctionHook> _logger;
    
    public AgentSkillsFunctionHook(
        IServiceProvider services,
        AgentSettings settings,
        ISkillService skillService,
        ILogger<AgentSkillsFunctionHook> logger)
        : base(services, settings)
    {
        _skillService = skillService ?? throw new ArgumentNullException(nameof(skillService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// 函数加载时注册技能工具
    /// 实现需求: FR-3.1
    /// </summary>
    public override bool OnFunctionsLoaded(List<FunctionDef> functions)
    {
        try
        {
            // 获取 AgentSkillsDotNet 生成的工具
            var tools = _skillService.GetTools();
            
            _logger.LogDebug("Registering {Count} skill tools", tools.Count);
            
            // 转换为 BotSharp 的 FunctionDef
            foreach (var tool in tools)
            {
                if (tool is AIFunction aiFunc)
                {
                    var def = new FunctionDef
                    {
                        Name = aiFunc.Name,
                        Description = aiFunc.Description,
                        Parameters = ConvertToFunctionParametersDef(aiFunc.AdditionalProperties)
                    };
                    
                    // 防止重复添加
                    if (!functions.Any(f => f.Name == def.Name))
                    {
                        functions.Add(def);
                        _logger.LogDebug("Registered skill tool: {ToolName}", def.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Tool {ToolName} already registered, skipping", def.Name);
                    }
                }
            }
            
            _logger.LogInformation("Successfully registered {Count} skill tools", tools.Count);
        }
        catch (Exception ex)
        {
            // 工具注册失败不应中断 Agent 加载
            _logger.LogError(ex, "Failed to register skill tools");
        }
        
        return base.OnFunctionsLoaded(functions);
    }
    
    /// <summary>
    /// 将 AIFunction 的 AdditionalProperties 转换为 FunctionParametersDef
    /// </summary>
    private FunctionParametersDef? ConvertToFunctionParametersDef(
        IReadOnlyDictionary<string, object?> additionalProperties)
    {
        if (additionalProperties == null || additionalProperties.Count == 0)
        {
            return null;
        }
        
        try
        {
            // 序列化为 JSON 并解析为 JsonDocument
            var json = JsonSerializer.Serialize(additionalProperties);
            var doc = JsonDocument.Parse(json);
            
            // 提取 required 字段（如果存在）
            var required = new List<string>();
            if (additionalProperties.TryGetValue("required", out var requiredObj) 
                && requiredObj is JsonElement requiredElement 
                && requiredElement.ValueKind == JsonValueKind.Array)
            {
                required = requiredElement.EnumerateArray()
                    .Select(e => e.GetString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList()!;
            }
            
            return new FunctionParametersDef
            {
                Type = "object",
                Properties = doc,
                Required = required
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert AdditionalProperties to FunctionParametersDef");
            return null;
        }
    }
}
```

**设计决策：**
1. **重复检查**: 防止重复注册同名工具（NFR-2.1）
2. **异常处理**: 工具注册失败不中断 Agent 加载（FR-1.3）
3. **参数转换**: 正确处理 AIFunction 的参数定义（FR-3.1）
4. **日志记录**: 记录每个工具的注册状态（NFR-2.2）

### 2.5 插件注册

**需求追溯**: FR-1.1, FR-3.1, FR-4.1, NFR-2.1, NFR-4.1

**AgentSkillsPlugin.RegisterDI 完整实现：**
```csharp
/// <summary>
/// Agent Skills 插件
/// 实现需求: FR-1.1, FR-3.1, FR-4.1
/// </summary>
public class AgentSkillsPlugin : IBotSharpPlugin
{
    public string Id => "a5b3e8c1-7d2f-4a9e-b6c4-8f5d1e2a3b4c";
    public string Name => "Agent Skills";
    public string Description => "Enables AI agents to leverage reusable skills following the Agent Skills specification (https://agentskills.io).";
    public string IconUrl => "https://raw.githubusercontent.com/SciSharp/BotSharp/master/docs/static/logos/BotSharp.png";
    public string[] AgentIds => [];

    /// <summary>
    /// 注册依赖注入
    /// 实现需求: FR-1.1, FR-3.1, FR-4.1, NFR-4.1
    /// </summary>
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // FR-6.1: 注册配置
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<AgentSkillsSettings>("AgentSkills");
        });
        
        // FR-1.1: 注册 AgentSkillsFactory（单例）
        // 单例模式避免重复创建工厂实例
        services.AddSingleton<AgentSkillsFactory>();
        
        // FR-1.1, NFR-4.1: 注册技能服务（单例）
        // 单例模式确保技能只加载一次，提高性能
        services.AddSingleton<ISkillService, SkillService>();
        
        // FR-4.1: 初始化技能并注册工具
        // 注意：这里需要在服务注册完成后才能获取服务
        // 使用延迟初始化或启动时初始化
        services.AddHostedService<SkillInitializationService>();
        
        // FR-2.1: 注册指令注入钩子
        services.AddScoped<IAgentHook, AgentSkillsInstructionHook>();
        
        // FR-3.1: 注册函数注册钩子
        services.AddScoped<IAgentHook, AgentSkillsFunctionHook>();
    }
}

/// <summary>
/// 技能初始化服务
/// 实现需求: FR-1.1, FR-4.1
/// </summary>
public class SkillInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SkillInitializationService> _logger;

    public SkillInitializationService(
        IServiceProvider serviceProvider,
        ILogger<SkillInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Initializing Agent Skills...");
            
            // 获取技能服务（触发技能加载）
            var skillService = _serviceProvider.GetRequiredService<ISkillService>();
            var tools = skillService.GetTools();
            
            // FR-4.1: 将 AITool 注册为 IFunctionCallback
            var serviceCollection = new ServiceCollection();
            foreach (var tool in tools)
            {
                if (tool is AIFunction aiFunc)
                {
                    // 注册为 Scoped，每次请求创建新实例
                    serviceCollection.AddScoped<IFunctionCallback>(provider =>
                        new AIToolCallbackAdapter(
                            aiFunc, 
                            provider,
                            provider.GetService<ILogger<AIToolCallbackAdapter>>()));
                }
            }
            
            _logger.LogInformation(
                "Agent Skills initialized successfully. Loaded {SkillCount} skills, registered {ToolCount} tools",
                skillService.GetSkillCount(),
                tools.Count);
        }
        catch (Exception ex)
        {
            // FR-1.3: 初始化失败不应中断应用启动
            _logger.LogError(ex, "Failed to initialize Agent Skills");
        }
        
        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Agent Skills...");
        return Task.CompletedTask;
    }
}
```

**替代方案（简化版）：**
如果不使用 IHostedService，可以在 RegisterDI 中直接注册工具：
```csharp
public void RegisterDI(IServiceCollection services, IConfiguration config)
{
    // ... 前面的注册代码 ...
    
    // 构建临时服务提供者以获取技能服务
    using (var sp = services.BuildServiceProvider())
    {
        try
        {
            var skillService = sp.GetRequiredService<ISkillService>();
            var tools = skillService.GetTools();
            
            // FR-4.1: 注册工具
            foreach (var tool in tools)
            {
                if (tool is AIFunction aiFunc)
                {
                    // 捕获 aiFunc 到闭包中
                    var capturedFunc = aiFunc;
                    services.AddScoped<IFunctionCallback>(provider =>
                        new AIToolCallbackAdapter(
                            capturedFunc, 
                            provider,
                            provider.GetService<ILogger<AIToolCallbackAdapter>>()));
                }
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不中断注册
            var logger = sp.GetService<ILogger<AgentSkillsPlugin>>();
            logger?.LogError(ex, "Failed to register skill tools");
        }
    }
    
    // ... 注册钩子 ...
}
```

**关键点：**
1. **AgentSkillsFactory 单例**: 避免重复创建工厂实例（NFR-1.1）
2. **SkillService 单例**: 确保技能只加载一次，提高性能（NFR-1.1）
3. **AIToolCallbackAdapter Scoped**: 每次请求创建新实例，避免状态共享（NFR-2.1）
4. **延迟初始化**: 使用 IHostedService 或临时服务提供者（FR-1.1）
5. **错误容忍**: 初始化失败不中断应用启动（FR-1.3）

**性能考虑：**
- 技能在应用启动时加载一次（NFR-1.1）
- 工具定义在启动时注册一次（NFR-1.1）
- 工具执行时创建新的适配器实例（避免状态污染）

## 3. 配置设计

**需求追溯**: FR-6.1, FR-6.2

### 3.1 配置结构

```json
{
  "AgentSkills": {
    "EnableUserSkills": true,
    "EnableProjectSkills": true,
    "UserSkillsDir": null,
    "ProjectSkillsDir": null,
    "CacheSkills": true,
    "ValidateOnStartup": false,
    "SkillsCacheDurationSeconds": 300,
    "EnableReadSkillTool": true,
    "EnableReadFileTool": true,
    "EnableListDirectoryTool": true,
    "MaxOutputSizeBytes": 51200
  }
}
```

### 3.2 配置说明

| 配置项 | 类型 | 默认值 | 需求 | 说明 |
|--------|------|--------|------|------|
| EnableUserSkills | bool | true | FR-1.2 | 启用用户级技能（~/.botsharp/skills/） |
| EnableProjectSkills | bool | true | FR-1.2 | 启用项目级技能（{project}/.botsharp/skills/） |
| UserSkillsDir | string? | null | FR-1.2 | 自定义用户技能目录，null 使用默认路径 |
| ProjectSkillsDir | string? | null | FR-1.2 | 自定义项目技能目录，null 使用默认路径 |
| CacheSkills | bool | true | NFR-1.3 | 启用技能缓存（由 AgentSkillsDotNet 管理） |
| ValidateOnStartup | bool | false | FR-6.2 | 启动时验证技能（可选，影响启动时间） |
| SkillsCacheDurationSeconds | int | 300 | NFR-1.3 | 缓存持续时间（秒），0 表示永久缓存 |
| EnableReadSkillTool | bool | true | FR-3.2 | 启用 read_skill 工具 |
| EnableReadFileTool | bool | true | FR-3.2 | 启用 read_skill_file 工具 |
| EnableListDirectoryTool | bool | true | FR-3.2 | 启用 list_skill_directory 工具 |
| MaxOutputSizeBytes | int | 51200 | FR-5.2 | 最大输出大小（字节），50KB |

### 3.3 配置类实现

**需求追溯**: FR-6.1, FR-6.2

```csharp
/// <summary>
/// Agent Skills 插件配置
/// 实现需求: FR-6.1, FR-6.2
/// </summary>
public class AgentSkillsSettings
{
    /// <summary>
    /// 启用用户级技能
    /// 实现需求: FR-1.2
    /// </summary>
    public bool EnableUserSkills { get; set; } = true;

    /// <summary>
    /// 启用项目级技能
    /// 实现需求: FR-1.2
    /// </summary>
    public bool EnableProjectSkills { get; set; } = true;

    /// <summary>
    /// 自定义用户技能目录
    /// 实现需求: FR-1.2
    /// </summary>
    public string? UserSkillsDir { get; set; }

    /// <summary>
    /// 自定义项目技能目录
    /// 实现需求: FR-1.2
    /// </summary>
    public string? ProjectSkillsDir { get; set; }

    /// <summary>
    /// 启用技能缓存
    /// 实现需求: NFR-1.3
    /// </summary>
    public bool CacheSkills { get; set; } = true;

    /// <summary>
    /// 启动时验证技能
    /// 实现需求: FR-6.2
    /// </summary>
    public bool ValidateOnStartup { get; set; } = false;

    /// <summary>
    /// 技能缓存持续时间（秒）
    /// 实现需求: NFR-1.3
    /// </summary>
    public int SkillsCacheDurationSeconds { get; set; } = 300;

    /// <summary>
    /// 启用 read_skill 工具
    /// 实现需求: FR-3.2
    /// </summary>
    public bool EnableReadSkillTool { get; set; } = true;

    /// <summary>
    /// 启用 read_skill_file 工具
    /// 实现需求: FR-3.2
    /// </summary>
    public bool EnableReadFileTool { get; set; } = true;

    /// <summary>
    /// 启用 list_skill_directory 工具
    /// 实现需求: FR-3.2
    /// </summary>
    public bool EnableListDirectoryTool { get; set; } = true;

    /// <summary>
    /// 最大输出大小（字节）
    /// 实现需求: FR-5.2
    /// </summary>
    public int MaxOutputSizeBytes { get; set; } = 50 * 1024; // 50KB

    /// <summary>
    /// 获取用户技能目录路径
    /// 实现需求: FR-1.2
    /// </summary>
    public string GetUserSkillsDirectory()
    {
        if (!string.IsNullOrEmpty(UserSkillsDir))
        {
            return UserSkillsDir;
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".botsharp", "skills");
    }

    /// <summary>
    /// 获取项目技能目录路径
    /// 实现需求: FR-1.2
    /// </summary>
    public string? GetProjectSkillsDirectory(string? projectRoot = null)
    {
        if (!string.IsNullOrEmpty(ProjectSkillsDir))
        {
            return ProjectSkillsDir;
        }

        if (string.IsNullOrEmpty(projectRoot))
        {
            projectRoot = Directory.GetCurrentDirectory();
        }

        return Path.Combine(projectRoot, ".botsharp", "skills");
    }

    /// <summary>
    /// 验证配置
    /// 实现需求: FR-6.2
    /// </summary>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (MaxOutputSizeBytes <= 0)
        {
            errors.Add("MaxOutputSizeBytes must be greater than 0");
        }

        if (SkillsCacheDurationSeconds < 0)
        {
            errors.Add("SkillsCacheDurationSeconds must be non-negative");
        }

        if (!EnableUserSkills && !EnableProjectSkills)
        {
            errors.Add("At least one of EnableUserSkills or EnableProjectSkills must be true");
        }

        return errors;
    }
}
```

## 4. 安全设计

### 4.1 路径安全

AgentSkillsDotNet 库已内置路径安全验证：
- 自动验证所有文件路径
- 禁止目录遍历（../, ..\）
- 限制访问范围在技能目录内

**我们的职责：**
- 确保传递给库的目录路径是安全的
- 验证配置的技能目录路径

### 4.2 资源限制

AgentSkillsDotNet 库支持通过 AgentSkillsAsToolsOptions 配置：
```csharp
new AgentSkillsAsToolsOptions
{
    MaxOutputSizeBytes = _settings.MaxOutputSizeBytes
}
```

**我们的职责：**
- 在配置中设置合理的限制值
- 监控内存使用

### 4.3 日志审计

**实现要点：**
- 记录技能加载操作
- 记录工具调用（通过 AIToolCallbackAdapter）
- 记录异常和错误
- 使用 BotSharp 的日志框架

```csharp
public class SkillService : ISkillService
{
    private readonly ILogger<SkillService> _logger;
    
    private void InitializeSkills()
    {
        _logger.LogInformation("Loading skills from {Directory}", projectSkillsDir);
        
        try
        {
            _agentSkills = _factory.GetAgentSkills(projectSkillsDir);
            _logger.LogInformation("Loaded {Count} skills", _agentSkills.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load skills from {Directory}", projectSkillsDir);
            throw;
        }
    }
}
```

## 5. 错误处理

### 5.1 错误类型

AgentSkillsDotNet 库会抛出的异常：
- 技能不存在
- 文件访问失败
- 文件大小超限
- 路径安全违规

**我们的处理策略：**
```csharp
public class AIToolCallbackAdapter : IFunctionCallback
{
    public async Task<bool> Execute(RoleDialogModel message)
    {
        try
        {
            var result = await _aiFunction.InvokeAsync(aiArgs);
            message.Content = result.ConvertToString();
            return true;
        }
        catch (FileNotFoundException ex)
        {
            message.Content = $"Skill or file not found: {ex.Message}";
            _logger.LogWarning(ex, "Skill file not found");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            message.Content = $"Access denied: {ex.Message}";
            _logger.LogError(ex, "Unauthorized access attempt");
            return false;
        }
        catch (Exception ex)
        {
            message.Content = $"Error executing tool {Name}: {ex.Message}";
            _logger.LogError(ex, "Tool execution failed");
            return false;
        }
    }
}
```

### 5.2 错误处理策略

- **启动阶段**：记录警告，继续加载其他技能
- **运行阶段**：返回友好错误消息给 Agent
- **关键错误**：抛出异常，中断操作

## 6. 性能优化

**需求追溯**: NFR-1.1, NFR-1.2, NFR-1.3

### 6.1 缓存策略

**实现需求**: NFR-1.3

AgentSkillsDotNet 库内置缓存机制，我们通过配置控制：

```csharp
// 在 SkillService 中
public class SkillService : ISkillService
{
    private AgentSkills? _agentSkills; // 缓存技能实例
    private IList<AITool>? _tools;     // 缓存工具列表
    
    // 技能实例在构造函数中创建，整个应用生命周期内复用
}
```

**缓存层次：**
1. **应用级缓存**: SkillService 单例，技能实例在应用启动时创建
2. **库级缓存**: AgentSkillsDotNet 内部缓存 SKILL.md 内容
3. **配置控制**: 通过 CacheSkills 和 SkillsCacheDurationSeconds 配置

**缓存失效：**
- 手动调用 `ReloadSkillsAsync()` 方法
- 应用重启
- 配置的缓存时间到期（由 AgentSkillsDotNet 管理）

### 6.2 延迟加载

**实现需求**: NFR-1.1, NFR-1.2

**元数据加载（启动时）：**
```csharp
// 仅加载 frontmatter，不加载完整内容
_agentSkills = _factory.GetAgentSkills(projectSkillsDir);
// AgentSkillsDotNet 库只解析 YAML frontmatter
```

**完整内容加载（按需）：**
```csharp
// Agent 调用 read_skill 工具时才加载完整内容
await _aiFunction.InvokeAsync(aiArgs); // 内部读取完整 SKILL.md
```

**性能指标：**
- 启动时元数据加载: < 1秒（100个技能）（NFR-1.1）
- 单个技能内容读取: < 100ms（NFR-1.2）
- 内存占用: 元数据约 5-10KB/技能，完整内容按需加载

### 6.3 并发处理

**实现需求**: NFR-1.1

```csharp
public class SkillService : ISkillService
{
    private readonly object _lock = new object();
    
    private void InitializeSkills()
    {
        lock (_lock)
        {
            // 线程安全的技能加载
        }
    }
    
    public async Task ReloadSkillsAsync()
    {
        await Task.Run(() => InitializeSkills());
    }
}
```

**并发策略：**
- 技能加载使用锁保护，避免并发加载
- 工具执行支持并发（AIFunction.InvokeAsync 是线程安全的）
- AIToolCallbackAdapter 注册为 Scoped，每次请求独立实例

**性能考虑：**
- 避免在请求处理路径中加载技能
- 使用异步 I/O 读取文件
- 最小化锁的持有时间

## 7. 测试策略

### 7.1 单元测试

**测试 SkillService：**
- 技能加载测试
- 多目录合并测试
- 配置驱动测试
- 错误处理测试

**测试 AIToolCallbackAdapter：**
- 参数解析测试
- 工具调用测试
- 错误处理测试

**测试钩子：**
- 指令注入测试
- 函数注册测试
- Agent 类型过滤测试

### 7.2 集成测试

- 完整插件加载流程测试
- 与 AgentSkillsDotNet 库集成测试
- 与 BotSharp 框架集成测试

### 7.3 测试数据

使用 AgentSkillsDotNet 库提供的示例技能或创建自定义测试技能：
- valid-skill: 完全符合规范的技能
- skill-with-scripts: 包含脚本的技能
- skill-with-references: 包含参考文档的技能

### 7.4 模拟 AgentSkillsDotNet

对于单元测试，可以模拟 AgentSkillsFactory 和 AgentSkills：
```csharp
var mockFactory = new Mock<AgentSkillsFactory>();
var mockSkills = new Mock<AgentSkills>();
mockSkills.Setup(s => s.GetInstructions()).Returns("<available_skills>...</available_skills>");
mockFactory.Setup(f => f.GetAgentSkills(It.IsAny<string>())).Returns(mockSkills.Object);
```

## 8. 迁移计划

### 8.1 向后兼容

- 保留现有 AgentSkillsSettings 配置项
- 保留 AIToolCallbackAdapter（标记为过时）
- 提供迁移指南

### 8.2 迁移步骤

1. 部署新版本插件
2. 更新配置文件（可选）
3. 验证技能加载正常
4. 逐步移除旧代码

## 9. 未来扩展

### 9.1 脚本执行

- 设计脚本执行接口
- 实现沙箱环境
- 支持多语言脚本（Python, Bash, PowerShell）

### 9.2 技能市场

- 技能仓库集成
- 技能下载和安装
- 技能版本管理

### 9.3 高级功能

- 技能依赖解析
- 技能热重载
- 技能使用统计

## 10. 参考实现

**需求追溯**: NFR-3.2

### 10.1 AgentSkillsDotNet 库 API

本设计基于 AgentSkillsDotNet 库的以下 API：

**核心类：**
```csharp
// 技能工厂
public class AgentSkillsFactory
{
    public AgentSkills GetAgentSkills(string? skillsDir);
}

// 技能集合
public class AgentSkills
{
    public int Count { get; }
    public string GetInstructions();
    public IList<AITool> GetAsTools(
        AgentSkillsAsToolsStrategy strategy, 
        AgentSkillsAsToolsOptions options);
}

// 工具策略
public enum AgentSkillsAsToolsStrategy
{
    AvailableSkillsOnly,
    AvailableSkillsAndLookupTools
}

// 工具选项
public class AgentSkillsAsToolsOptions
{
    public bool IncludeToolForFileContentRead { get; set; }
    public bool IncludeToolForDirectoryListing { get; set; }
    public int MaxOutputSizeBytes { get; set; }
}
```

### 10.2 使用示例

```csharp
// 1. 创建工厂
var factory = new AgentSkillsFactory();

// 2. 加载技能
var skills = factory.GetAgentSkills("/path/to/skills");

// 3. 获取指令
var instructions = skills.GetInstructions();
// 返回: <available_skills>...</available_skills>

// 4. 生成工具
var tools = skills.GetAsTools(
    AgentSkillsAsToolsStrategy.AvailableSkillsAndLookupTools,
    new AgentSkillsAsToolsOptions
    {
        IncludeToolForFileContentRead = true,
        MaxOutputSizeBytes = 51200
    }
);

// 5. 使用工具
foreach (var tool in tools)
{
    if (tool is AIFunction func)
    {
        var result = await func.InvokeAsync(args);
    }
}
```

### 10.3 与 BotSharp 集成

```csharp
// 1. 注册服务
services.AddSingleton<AgentSkillsFactory>();
services.AddSingleton<ISkillService, SkillService>();

// 2. 注册工具
var tools = skillService.GetTools();
foreach (var tool in tools)
{
    services.AddScoped<IFunctionCallback>(sp =>
        new AIToolCallbackAdapter(tool as AIFunction, sp));
}

// 3. 注册钩子
services.AddScoped<IAgentHook, AgentSkillsInstructionHook>();
services.AddScoped<IAgentHook, AgentSkillsFunctionHook>();
```

## 11. 正确性属性

**需求追溯**: NFR-2.3

以下属性用于属性测试（Property-Based Testing），验证系统的正确性。

### 11.1 技能加载属性

**属性 1.1**: 技能加载幂等性（FR-1.1）
```
对于任何有效的技能目录 dir，
多次调用 GetAgentSkills(dir) 应返回相同的技能集合
```

**属性 1.2**: 技能数量一致性（FR-1.1）
```
对于任何技能目录 dir，
GetAgentSkills(dir).Count 应等于目录中有效 SKILL.md 文件的数量
```

### 11.2 工具生成属性

**属性 2.1**: 工具名称唯一性（FR-3.1）
```
对于任何技能集合 skills，
GetAsTools(skills) 返回的工具名称应该是唯一的
```

**属性 2.2**: 工具配置一致性（FR-3.2）
```
IF EnableReadSkillTool = false，
THEN GetAsTools() 返回的工具列表不应包含 "read_skill"
```

### 11.3 路径安全属性

**属性 3.1**: 路径遍历防护（FR-5.1）
```
对于任何包含 "../" 或 "..\" 的路径 path，
read_skill_file(skill_name, path) 应抛出异常或返回错误
```

**属性 3.2**: 访问范围限制（FR-5.1）
```
对于任何技能 skill 和文件路径 path，
IF path 不在 skill 目录内，
THEN read_skill_file(skill, path) 应失败
```

### 11.4 文件大小属性

**属性 4.1**: 大小限制强制（FR-5.2）
```
对于任何文件 file，
IF file.Size > MaxOutputSizeBytes，
THEN read_skill_file(skill, file) 应抛出异常
```

### 11.5 指令注入属性

**属性 5.1**: Agent 类型过滤（FR-2.2）
```
对于任何 Agent agent，
IF agent.Type IN [Routing, Planning]，
THEN OnInstructionLoaded() 不应注入 available_skills
```

**属性 5.2**: 指令格式正确性（FR-2.1）
```
对于任何技能集合 skills，
GetInstructions() 应返回有效的 XML 格式字符串
```

### 11.6 错误处理属性

**属性 6.1**: 错误容忍性（FR-1.3）
```
对于任何无效的技能目录 dir，
GetAgentSkills(dir) 不应抛出未捕获的异常
```

**属性 6.2**: 部分失败恢复（FR-1.3）
```
对于包含 N 个技能的目录，其中 M 个无效，
GetAgentSkills() 应成功加载 (N - M) 个有效技能
```

## 12. 测试框架

**需求追溯**: NFR-2.3

使用以下测试框架和工具：

### 12.1 单元测试
- **xUnit**: 测试框架
- **FluentAssertions**: 断言库
- **Moq**: 模拟框架

### 12.2 属性测试
- **FsCheck**: 属性测试库（如果使用 F#）
- **CsCheck**: 属性测试库（C# 原生）

### 12.3 集成测试
- **Microsoft.AspNetCore.Mvc.Testing**: Web 应用测试
- **Testcontainers**: 容器化测试环境

### 12.4 测试覆盖率
- **Coverlet**: 代码覆盖率工具
- **ReportGenerator**: 覆盖率报告生成

**目标覆盖率**: > 80%（NFR-2.3）

## 13. 设计决策记录

### 决策 1: 使用 AgentSkillsDotNet 库
**日期**: 2026-01-28  
**状态**: 已接受  
**背景**: 需要实现 Agent Skills 规范  
**决策**: 基于 AgentSkillsDotNet 库实现，而不是从头开发  
**理由**:
- 库已实现规范的核心功能
- 减少开发和维护成本
- 确保规范兼容性
**后果**:
- 依赖外部库
- 受库 API 限制
- 需要适配到 BotSharp 框架

### 决策 2: 单例 SkillService
**日期**: 2026-01-28  
**状态**: 已接受  
**背景**: 技能加载性能优化  
**决策**: SkillService 注册为单例  
**理由**:
- 技能在应用生命周期内不变
- 避免重复加载提高性能
- 减少内存占用
**后果**:
- 技能更新需要重启应用
- 需要线程安全保护

### 决策 3: Scoped AIToolCallbackAdapter
**日期**: 2026-01-28  
**状态**: 已接受  
**背景**: 工具执行隔离  
**决策**: AIToolCallbackAdapter 注册为 Scoped  
**理由**:
- 每次请求独立实例
- 避免状态共享
- 支持依赖注入
**后果**:
- 每次请求创建新实例（轻微性能开销）
- 更好的隔离性和可测试性

### 决策 4: 不实现技能验证服务
**日期**: 2026-01-28  
**状态**: 已接受  
**背景**: AgentSkillsDotNet 库已提供验证  
**决策**: 不单独实现 SkillValidationService  
**理由**:
- AgentSkillsDotNet 库在加载时自动验证
- 避免重复实现
- 减少代码复杂度
**后果**:
- 依赖库的验证逻辑
- 无法自定义验证规则

## 14. 未来扩展

**需求追溯**: EX-1 到 EX-5

### 14.1 脚本执行（EX-1）
- 设计脚本执行接口
- 实现沙箱环境（Docker, WebAssembly）
- 支持多语言脚本（Python, Bash, PowerShell）
- 安全审查和权限控制

### 14.2 技能市场（EX-4）
- 技能仓库集成
- 技能下载和安装
- 技能版本管理（EX-2）
- 技能评分和评论

### 14.3 高级功能
- 技能依赖解析（EX-3）
- 技能热重载（EX-5）
- 技能使用统计
- 技能推荐系统

### 14.4 多租户支持
- 租户级技能隔离
- 技能访问控制
- 技能配额管理
