---
feature: agent-skills-refactor
created: 2026-01-28
status: draft
---

# Agent Skills 插件重构需求 (EARS 格式)

## 1. 概述

重构 BotSharp.Plugin.AgentSkills 插件，基于 AgentSkillsDotNet 库完整实现 [Agent Skills 规范](https://agentskills.io)，提供标准化的技能发现、加载和执行机制。

## 2. 背景

当前实现基于 AgentSkillsDotNet 库，但未充分利用库的功能和最佳实践。需要重构以：
- 完整利用 AgentSkillsDotNet 库的 API
- 支持渐进式披露（Progressive Disclosure）
- 提供工具化访问（Tool-based Access）
- 增强安全性和可扩展性

## 3. 功能需求 (EARS 格式)

### 3.1 技能发现与加载

#### FR-1.1 启动时技能扫描 (Event-driven)
**WHEN** 系统启动时，**the system shall** 使用 AgentSkillsFactory 扫描配置的技能目录并加载所有有效技能。

**验收标准：**
- 系统调用 `AgentSkillsFactory.GetAgentSkills(skillsDir)` 加载技能
- 仅加载 SKILL.md 的 frontmatter 元数据（name, description）
- 每个技能的元数据占用约 50-100 tokens

#### FR-1.2 多目录支持 (Optional)
**WHERE** 用户级技能已启用，**the system shall** 从 `~/.botsharp/skills/` 目录加载技能。

**WHERE** 项目级技能已启用，**the system shall** 从 `{project}/.botsharp/skills/` 目录加载技能。

**验收标准：**
- 支持通过配置启用/禁用用户级技能
- 支持通过配置启用/禁用项目级技能
- 支持自定义技能目录路径

#### FR-1.3 加载失败处理 (Unwanted behavior)
**IF** 技能目录不存在或无法访问，**THEN the system shall** 记录警告日志并继续启动，不中断系统。

**IF** 单个技能加载失败，**THEN the system shall** 记录该技能的错误信息并继续加载其他技能。

**验收标准：**
- 使用日志框架记录警告和错误
- 系统启动不因技能加载失败而中断

### 3.2 技能元数据注入

#### FR-2.1 指令注入 (Event-driven)
**WHEN** Agent 指令加载时，**the system shall** 调用 `AgentSkills.GetInstructions()` 获取技能列表并注入到 Agent 指令中。

**验收标准：**
- 技能元数据以 XML 格式注入（`<available_skills>` 标签）
- 包含技能名称和描述
- 元数据保持简洁，避免上下文膨胀

#### FR-2.2 Agent 类型过滤 (State-driven)
**WHILE** Agent 类型为 Routing 或 Planning，**the system shall** 跳过技能元数据注入。

**验收标准：**
- Routing 和 Planning 类型的 Agent 不接收技能列表
- 其他类型的 Agent 正常接收技能列表

### 3.3 技能激活（渐进式披露）

#### FR-3.1 按需加载工具 (Ubiquitous)
**The system shall** 使用 `AgentSkills.GetAsTools()` 方法生成技能访问工具，包括：
- `read_skill`: 读取完整 SKILL.md 内容
- `read_skill_file`: 读取技能目录中的文件
- `list_skill_directory`: 列出技能目录内容

**验收标准：**
- 使用 `AgentSkillsAsToolsStrategy.AvailableSkillsAndLookupTools` 策略
- 工具通过 AgentSkillsDotNet 库自动生成
- 工具符合 Agent Skills 规范

#### FR-3.2 工具配置 (Optional)
**WHERE** `EnableReadSkillTool` 配置为 true，**the system shall** 包含 `read_skill` 工具。

**WHERE** `EnableReadFileTool` 配置为 true，**the system shall** 包含 `read_skill_file` 工具。

**WHERE** `EnableListDirectoryTool` 配置为 true，**the system shall** 包含 `list_skill_directory` 工具。

**验收标准：**
- 通过 `AgentSkillsAsToolsOptions` 配置工具可用性
- 配置变更后需重启生效

### 3.4 工具执行

#### FR-4.1 工具适配 (Ubiquitous)
**The system shall** 使用 AIToolCallbackAdapter 将 AgentSkillsDotNet 生成的 AIFunction 适配为 BotSharp 的 IFunctionCallback。

**验收标准：**
- 适配器正确解析 JSON 参数
- 适配器调用 AIFunction.InvokeAsync() 执行工具
- 适配器将结果转换为字符串返回

#### FR-4.2 参数解析 (Event-driven)
**WHEN** Agent 调用工具时，**the system shall** 解析 JSON 格式的参数并传递给 AIFunction。

**验收标准：**
- 支持大小写不敏感的参数名称
- 参数解析失败时返回友好错误消息

#### FR-4.3 错误处理 (Unwanted behavior)
**IF** 工具执行失败，**THEN the system shall** 捕获异常并返回错误消息给 Agent。

**验收标准：**
- 捕获 FileNotFoundException、UnauthorizedAccessException 等异常
- 返回友好的错误消息
- 记录错误日志

### 3.5 安全性

#### FR-5.1 路径安全 (Ubiquitous)
**The system shall** 依赖 AgentSkillsDotNet 库的内置路径安全验证，防止目录遍历攻击。

**验收标准：**
- 禁止访问包含 `../` 或 `..\` 的路径
- 限制访问范围在技能目录内
- AgentSkillsDotNet 库自动处理路径安全

#### FR-5.2 文件大小限制 (Ubiquitous)
**The system shall** 通过 `AgentSkillsAsToolsOptions.MaxOutputSizeBytes` 配置限制文件读取大小。

**验收标准：**
- 默认限制为 50KB
- 超过限制时抛出异常
- 可通过配置调整限制值

#### FR-5.3 访问审计 (Event-driven)
**WHEN** 技能被加载或工具被调用时，**the system shall** 记录操作日志。

**验收标准：**
- 记录技能加载操作（目录、数量）
- 记录工具调用（工具名称、参数）
- 记录异常和错误
- 使用 BotSharp 日志框架

### 3.6 配置管理

#### FR-6.1 配置加载 (Ubiquitous)
**The system shall** 从 `appsettings.json` 的 `AgentSkills` 节点加载配置。

**验收标准：**
- 使用 ISettingService.Bind<AgentSkillsSettings>() 加载配置
- 支持所有配置项的默认值

#### FR-6.2 配置项 (Ubiquitous)
**The system shall** 支持以下配置项：
- `EnableUserSkills`: 启用用户级技能（默认 true）
- `EnableProjectSkills`: 启用项目级技能（默认 true）
- `UserSkillsDir`: 自定义用户技能目录（可选）
- `ProjectSkillsDir`: 自定义项目技能目录（可选）
- `CacheSkills`: 启用技能缓存（默认 true）
- `ValidateOnStartup`: 启动时验证技能（默认 true）
- `SkillsCacheDurationSeconds`: 缓存持续时间（默认 300 秒）
- `EnableReadSkillTool`: 启用 read_skill 工具（默认 true）
- `EnableReadFileTool`: 启用 read_skill_file 工具（默认 true）
- `EnableListDirectoryTool`: 启用 list_skill_directory 工具（默认 true）
- `MaxOutputSizeBytes`: 最大输出大小（默认 51200 字节）

**验收标准：**
- 所有配置项都有合理的默认值
- 配置验证失败时记录错误

## 4. 非功能性需求 (EARS 格式)

### 4.1 性能需求

#### NFR-1.1 启动性能 (Ubiquitous)
**The system shall** 在 1 秒内完成 100 个技能的元数据加载。

**验收标准：**
- 启动时间测量包括技能发现和元数据解析
- 使用性能测试验证

#### NFR-1.2 响应性能 (Ubiquitous)
**The system shall** 在 100 毫秒内完成单个技能内容的读取。

**验收标准：**
- 响应时间测量从工具调用到返回结果
- 不包括网络延迟

#### NFR-1.3 缓存性能 (Optional)
**WHERE** 技能缓存已启用，**the system shall** 从缓存中读取技能内容，避免重复文件 I/O。

**验收标准：**
- 缓存命中率 > 80%（正常使用场景）
- 缓存失效基于配置的时间间隔

### 4.2 可维护性需求

#### NFR-2.1 代码结构 (Ubiquitous)
**The system shall** 遵循单一职责原则，将技能服务、工具适配、钩子实现分离到不同的类。

**验收标准：**
- 每个类职责明确
- 类之间通过接口交互
- 符合 BotSharp 插件架构规范

#### NFR-2.2 日志记录 (Ubiquitous)
**The system shall** 在关键操作点记录详细日志，包括：
- 技能加载（信息级别）
- 工具调用（调试级别）
- 错误和异常（错误级别）

**验收标准：**
- 使用 BotSharp 日志框架
- 日志包含足够的上下文信息
- 日志级别可配置

#### NFR-2.3 测试覆盖 (Ubiquitous)
**The system shall** 提供完整的单元测试和集成测试。

**验收标准：**
- 单元测试覆盖率 > 80%
- 集成测试覆盖主要工作流
- 使用 xUnit 和 FluentAssertions

### 4.3 兼容性需求

#### NFR-3.1 规范兼容 (Ubiquitous)
**The system shall** 完全兼容 [Agent Skills 规范](https://agentskills.io/specification)。

**验收标准：**
- 支持所有必需的 frontmatter 字段
- 支持渐进式披露
- 支持工具化访问

#### NFR-3.2 库兼容 (Ubiquitous)
**The system shall** 基于 AgentSkillsDotNet 库实现，充分利用库提供的 API。

**验收标准：**
- 使用 AgentSkillsFactory 加载技能
- 使用 GetAsTools() 生成工具
- 使用 GetInstructions() 生成指令

#### NFR-3.3 框架兼容 (Ubiquitous)
**The system shall** 与 BotSharp 框架无缝集成。

**验收标准：**
- 实现 IBotSharpPlugin 接口
- 使用 IFunctionCallback 注册工具
- 使用 IAgentHook 注入指令和函数
- 支持 .NET 8.0+

### 4.4 可扩展性需求

#### NFR-4.1 服务扩展 (Ubiquitous)
**The system shall** 通过 ISkillService 接口提供技能服务，支持未来扩展。

**验收标准：**
- 接口定义清晰
- 实现可替换
- 支持依赖注入

#### NFR-4.2 配置扩展 (Ubiquitous)
**The system shall** 支持通过配置添加新的选项，无需修改代码。

**验收标准：**
- 配置类支持新增属性
- 向后兼容旧配置

#### NFR-4.3 工具扩展 (Optional)
**WHERE** 未来需要自定义工具，**the system shall** 支持在 AgentSkillsDotNet 生成的工具基础上添加自定义工具。

**验收标准：**
- 自定义工具可通过 IFunctionCallback 注册
- 不影响 AgentSkillsDotNet 生成的工具

## 5. 技术约束 (EARS 格式)

### TC-1 库依赖 (Ubiquitous)
**The system shall** 使用以下库和框架：
- AgentSkillsDotNet NuGet 包（核心功能）
- BotSharp.Core（框架集成）
- BotSharp.Abstraction（接口定义）
- Microsoft.Extensions.AI（工具定义）
- YamlDotNet（YAML 解析，AgentSkillsDotNet 依赖）

### TC-2 平台约束 (Ubiquitous)
**The system shall** 支持 .NET 8.0 及以上版本。

### TC-3 配置约束 (Ubiquitous)
**The system shall** 使用 BotSharp 的 ISettingService 加载配置。

## 6. 依赖关系

### 6.1 外部依赖
- **AgentSkillsDotNet**: 提供技能加载、工具生成、指令生成功能
- **Microsoft.Extensions.AI**: 提供 AIFunction 和 AITool 定义
- **BotSharp.Core**: 提供插件框架和服务注册
- **BotSharp.Abstraction**: 提供接口定义（IFunctionCallback, IAgentHook 等）

### 6.2 内部依赖
- SkillService 依赖 AgentSkillsFactory 和 AgentSkillsSettings
- AIToolCallbackAdapter 依赖 AIFunction 和 IServiceProvider
- 钩子依赖 ISkillService

## 7. 排除范围

以下功能不在本次重构范围内：

### EX-1 脚本执行 (Ubiquitous)
**The system shall NOT** 自动执行技能目录中的脚本文件。

**理由：** 需要沙箱环境和安全审查机制

### EX-2 技能版本管理 (Ubiquitous)
**The system shall NOT** 管理技能的版本和更新。

**理由：** 超出当前范围，可作为未来功能

### EX-3 技能依赖解析 (Ubiquitous)
**The system shall NOT** 解析和管理技能之间的依赖关系。

**理由：** Agent Skills 规范未定义依赖机制

### EX-4 技能市场集成 (Ubiquitous)
**The system shall NOT** 集成技能市场或仓库。

**理由：** 超出当前范围，可作为未来功能

### EX-5 技能热重载 (Ubiquitous)
**The system shall NOT** 支持运行时热重载技能。

**理由：** 需要复杂的状态管理，可作为未来功能

## 8. 参考资料

- [Agent Skills 官方网站](https://agentskills.io)
- [Agent Skills 规范](https://agentskills.io/specification)
- [集成指南](https://agentskills.io/integrate-skills)
- [AgentSkillsDotNet GitHub](https://github.com/agentskills/agentskills-dotnet)（假设存在）
- [Microsoft.Extensions.AI 文档](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai)

## 9. 术语表

- **Agent Skills**: 遵循 agentskills.io 规范的技能格式
- **SKILL.md**: 技能定义文件，包含 YAML frontmatter 和 Markdown 内容
- **Frontmatter**: SKILL.md 文件开头的 YAML 元数据
- **Progressive Disclosure**: 渐进式披露，先加载元数据，按需加载完整内容
- **Tool-based Access**: 通过工具（函数）访问技能内容
- **AgentSkillsDotNet**: .NET 实现的 Agent Skills 库
- **AIFunction**: Microsoft.Extensions.AI 定义的函数类型
- **IFunctionCallback**: BotSharp 定义的函数回调接口
