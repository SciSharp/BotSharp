---
feature: agent-skills-refactor
created: 2026-01-28
updated: 2026-01-28
status: draft
---

# Agent Skills 插件重构任务列表

## 任务说明

本任务列表基于 AgentSkillsDotNet 库实现 Agent Skills 规范。每个任务都标注了对应的需求编号（FR/NFR）和设计章节。

**关键原则**：
- 充分利用 AgentSkillsDotNet 库的功能，避免重复实现
- 专注于适配层：将 AgentSkillsDotNet 适配到 BotSharp 框架
- 遵循 EARS 格式的需求规范
- 确保所有代码可测试、可维护

## 1. 项目准备和环境配置

- [x] 1.1 验证 AgentSkillsDotNet 库依赖
  **需求**: TC-1  
  **设计**: 2.5  
  **详情**: 确认 AgentSkillsDotNet NuGet 包已正确引用
  - [x] 1.1.1 检查 BotSharp.Plugin.AgentSkills.csproj 中的包引用
  - [x] 1.1.2 验证 AgentSkillsDotNet 版本兼容性
  - [x] 1.1.3 确认 Microsoft.Extensions.AI 包引用
  - [x] 1.1.4 确认 YamlDotNet 包引用（AgentSkillsDotNet 依赖）

- [x] 1.2 创建测试技能示例
  **需求**: NFR-2.3  
  **设计**: 7.3  
  **详情**: 创建符合 Agent Skills 规范的测试技能
  - [x] 1.2.1 创建测试技能目录结构 (tests/test-skills/)
  - [x] 1.2.2 创建 valid-skill 示例（完整的 SKILL.md + scripts/ + references/ + assets/）
  - [x] 1.2.3 创建 minimal-skill 示例（仅 SKILL.md，最小化内容）
  - [x] 1.2.4 创建 skill-with-scripts 示例（包含 Python 和 Bash 脚本）
  - [x] 1.2.5 创建 large-content-skill 示例（测试文件大小限制，> 50KB）

- [x] 1.3 设置测试项目
  **需求**: NFR-2.3  
  **设计**: 12  
  **详情**: 配置单元测试和集成测试环境
  - [x] 1.3.1 创建或验证 BotSharp.Plugin.AgentSkills.Tests 项目存在
  - [x] 1.3.2 添加测试依赖包（xUnit, FluentAssertions, Moq, Coverlet）
  - [x] 1.3.3 配置测试数据目录和测试技能路径
  - [x] 1.3.4 添加 CsCheck 用于属性测试（可选）
  - [x] 1.3.5 配置代码覆盖率工具（Coverlet + ReportGenerator）

## 2. 配置管理实现

- [x] 2.1 更新 AgentSkillsSettings 类
  **需求**: FR-6.1, FR-6.2  
  **设计**: 3.3  
  **详情**: 完善配置类，添加验证方法
  - [x] 2.1.1 确认所有配置属性已定义（参考 design.md 3.3）
  - [x] 2.1.2 实现 Validate() 方法验证配置有效性
  - [x] 2.1.3 添加 XML 文档注释说明每个配置项
  - [x] 2.1.4 确保所有配置项有合理的默认值
  - [x] 2.1.5 实现 GetUserSkillsDirectory() 方法
  - [x] 2.1.6 实现 GetProjectSkillsDirectory() 方法

- [x] 2.2 编写配置单元测试
  **需求**: NFR-2.3  
  **设计**: 12.1  
  **详情**: 测试配置加载和验证
  - [x] 2.2.1 测试默认配置值
  - [x] 2.2.2 测试自定义配置加载（从 IConfiguration）
  - [x] 2.2.3 测试配置验证（无效值应返回错误）
  - [x] 2.2.4 测试目录路径解析
  - [x] 2.2.5 测试边界条件（MaxOutputSizeBytes = 0, 负数等）

## 3. 技能服务实现

- [x] 3.1 创建 ISkillService 接口
  **需求**: FR-1.1, NFR-4.1  
  **设计**: 2.1  
  **详情**: 定义技能服务接口
  - [x] 3.1.1 创建 Services/ISkillService.cs 文件
  - [x] 3.1.2 定义 GetAgentSkills() 方法
  - [x] 3.1.3 定义 GetInstructions() 方法
  - [x] 3.1.4 定义 GetTools() 方法
  - [x] 3.1.5 定义 ReloadSkillsAsync() 方法
  - [x] 3.1.6 定义 GetSkillCount() 方法
  - [x] 3.1.7 添加 XML 文档注释

- [x] 3.2 实现 SkillService 类
  **需求**: FR-1.1, FR-1.2, FR-1.3  
  **设计**: 2.1  
  **详情**: 封装 AgentSkillsDotNet 库功能
  - [x] 3.2.1 创建 Services/SkillService.cs 文件
  - [x] 3.2.2 实现构造函数（注入 AgentSkillsFactory, AgentSkillsSettings, ILogger）
  - [x] 3.2.3 实现 InitializeSkills() 私有方法
  - [x] 3.2.4 实现项目级技能加载（调用 GetAgentSkills）
  - [x] 3.2.5 实现用户级技能加载（如果 EnableUserSkills = true）
  - [x] 3.2.6 实现技能合并逻辑（如果需要支持多目录）
  - [x] 3.2.7 实现 GetAsTools() 调用，根据配置生成工具
  - [x] 3.2.8 实现错误处理（目录不存在时记录警告，继续启动）
  - [x] 3.2.9 实现线程安全（使用 lock 保护 InitializeSkills）
  - [x] 3.2.10 添加详细的日志记录（Info, Warning, Error）
  - [x] 3.2.11 实现所有接口方法（GetAgentSkills, GetInstructions, GetTools, ReloadSkillsAsync, GetSkillCount）

- [x] 3.3 编写 SkillService 单元测试
  **需求**: NFR-2.3  
  **设计**: 12.1  
  **详情**: 测试技能服务核心功能
  - [x] 3.3.1 测试技能加载成功（使用测试技能目录）
  - [x] 3.3.2 测试 GetInstructions() 返回有效 XML 格式
  - [x] 3.3.3 测试 GetTools() 返回工具列表
  - [x] 3.3.4 测试 GetSkillCount() 返回正确数量
  - [x] 3.3.5 测试目录不存在时的错误处理（应记录警告，不抛异常）
  - [x] 3.3.6 测试配置驱动的行为（EnableUserSkills, EnableProjectSkills）
  - [x] 3.3.7 测试 ReloadSkillsAsync() 方法
  - [x] 3.3.8 测试线程安全（并发调用 ReloadSkillsAsync）
  - [x] 3.3.9 使用 Moq 模拟 AgentSkillsFactory 和 AgentSkills

- [x] 3.4* 编写 SkillService 属性测试
  **需求**: NFR-2.3  
  **设计**: 11.1  
  **详情**: 使用属性测试验证正确性属性
  - [x] 3.4.1 属性测试：技能加载幂等性（属性 1.1）
  - [x] 3.4.2 属性测试：技能数量一致性（属性 1.2）

## 4. 工具适配器实现

- [x] 4.1 实现 AIToolCallbackAdapter 类
  **需求**: FR-4.1, FR-4.2, FR-4.3  
  **设计**: 2.3.2  
  **详情**: 适配 AIFunction 到 IFunctionCallback
  - [x] 4.1.1 创建 Functions/AIToolCallbackAdapter.cs 文件
  - [x] 4.1.2 实现构造函数（注入 AIFunction, IServiceProvider, ILogger）
  - [x] 4.1.3 实现 Name 属性（映射 AIFunction.Name）
  - [x] 4.1.4 实现 Provider 属性（返回 "AgentSkills"）
  - [x] 4.1.5 实现 Execute() 方法
  - [x] 4.1.6 实现 JSON 参数解析（PropertyNameCaseInsensitive = true）
  - [x] 4.1.7 实现 AIFunction.InvokeAsync() 调用
  - [x] 4.1.8 实现错误分类处理（FileNotFoundException, UnauthorizedAccessException, InvalidOperationException）
  - [x] 4.1.9 添加详细的日志记录（Debug, Info, Warning, Error）
  - [x] 4.1.10 实现友好的错误消息返回

- [x] 4.2 编写 AIToolCallbackAdapter 单元测试
  **需求**: NFR-2.3  
  **设计**: 12.1  
  **详情**: 测试适配器功能
  - [x] 4.2.1 测试正常执行流程（模拟 AIFunction 返回成功）
  - [x] 4.2.2 测试参数解析（有效 JSON）
  - [x] 4.2.3 测试参数解析失败（无效 JSON）
  - [x] 4.2.4 测试空参数和 null 参数处理
  - [x] 4.2.5 测试 FileNotFoundException 错误处理
  - [x] 4.2.6 测试 UnauthorizedAccessException 错误处理
  - [x] 4.2.7 测试文件大小超限错误处理
  - [x] 4.2.8 测试通用异常处理
  - [x] 4.2.9 测试日志记录（验证日志级别和内容）
  - [x] 4.2.10 使用 Moq 模拟 AIFunction

## 5. 钩子实现

- [x] 5.1 实现 AgentSkillsInstructionHook 类
  **需求**: FR-2.1, FR-2.2  
  **设计**: 2.4.1  
  **详情**: 注入技能列表到 Agent 指令
  - [x] 5.1.1 创建 Hooks/AgentSkillsInstructionHook.cs 文件
  - [x] 5.1.2 继承 AgentHookBase
  - [x] 5.1.3 实现构造函数（注入 ISkillService, ILogger）
  - [x] 5.1.4 实现 OnInstructionLoaded() 方法
  - [x] 5.1.5 实现 Agent 类型过滤（跳过 Routing 和 Planning）
  - [x] 5.1.6 调用 GetInstructions() 获取技能列表 XML
  - [x] 5.1.7 注入到 dict["available_skills"]
  - [x] 5.1.8 实现错误处理（注入失败不中断 Agent 加载）
  - [x] 5.1.9 添加日志记录（Debug, Info, Warning, Error）

- [x] 5.2 实现 AgentSkillsFunctionHook 类
  **需求**: FR-3.1  
  **设计**: 2.4.2  
  **详情**: 注册技能工具到 BotSharp
  - [x] 5.2.1 创建 Hooks/AgentSkillsFunctionHook.cs 文件
  - [x] 5.2.2 继承 AgentHookBase
  - [x] 5.2.3 实现构造函数（注入 ISkillService, ILogger）
  - [x] 5.2.4 实现 OnFunctionsLoaded() 方法
  - [x] 5.2.5 调用 GetTools() 获取工具列表
  - [x] 5.2.6 实现 AIFunction 到 FunctionDef 的转换
  - [x] 5.2.7 实现 ConvertToFunctionParametersDef() 私有方法
  - [x] 5.2.8 实现重复检查（防止重复注册同名工具）
  - [x] 5.2.9 实现错误处理和日志记录
  - [x] 5.2.10 处理 required 字段提取（从 AdditionalProperties）

- [x] 5.3 编写钩子单元测试
  **需求**: NFR-2.3  
  **设计**: 12.1  
  **详情**: 测试钩子功能
  - [x] 5.3.1 测试 AgentSkillsInstructionHook 指令注入成功
  - [x] 5.3.2 测试 Agent 类型过滤（Routing, Planning 应跳过）
  - [x] 5.3.3 测试其他 Agent 类型正常注入
  - [x] 5.3.4 测试 XML 格式正确性（验证 <available_skills> 标签）
  - [x] 5.3.5 测试 AgentSkillsFunctionHook 函数注册成功
  - [x] 5.3.6 测试参数转换正确性（FunctionParametersDef）
  - [x] 5.3.7 测试重复注册防护
  - [x] 5.3.8 测试错误处理（GetTools 失败）
  - [x] 5.3.9 使用 Moq 模拟 ISkillService 和 Agent

- [x] 5.4* 编写钩子属性测试
  **需求**: NFR-2.3  
  **设计**: 11.5, 11.2  
  **详情**: 验证钩子正确性属性
  - [x] 5.4.1 属性测试：Agent 类型过滤（属性 5.1）
  - [x] 5.4.2 属性测试：指令格式正确性（属性 5.2）
  - [x] 5.4.3 属性测试：工具名称唯一性（属性 2.1）

## 6. 插件集成

- [x] 6.1 更新 AgentSkillsPlugin 类
  **需求**: FR-1.1, FR-3.1, FR-4.1  
  **设计**: 2.5  
  **详情**: 实现插件注册逻辑
  - [x] 6.1.1 更新 AgentSkillsPlugin.cs 文件
  - [x] 6.1.2 更新 RegisterDI() 方法
  - [x] 6.1.3 注册 AgentSkillsSettings（使用 ISettingService.Bind）
  - [x] 6.1.4 注册 AgentSkillsFactory（单例）
  - [x] 6.1.5 注册 ISkillService 和 SkillService（单例）
  - [x] 6.1.6 注册钩子（AgentSkillsInstructionHook, AgentSkillsFunctionHook）
  - [x] 6.1.7 添加 XML 文档注释

- [x] 6.2 实现工具注册逻辑
  **需求**: FR-4.1  
  **设计**: 2.5  
  **详情**: 将 AIFunction 注册为 IFunctionCallback
  - [x] 6.2.1 选择实现方案（IHostedService 或简化版）
  - [x] 6.2.2 如果使用 IHostedService：创建 SkillInitializationService 类
  - [x] 6.2.3 实现 StartAsync() 方法（获取工具并注册）
  - [x] 6.2.4 实现 StopAsync() 方法
  - [x] 6.2.5 如果使用简化版：在 RegisterDI 中使用临时 ServiceProvider
  - [x] 6.2.6 遍历工具列表，注册 AIToolCallbackAdapter（Scoped）
  - [x] 6.2.7 实现错误处理（初始化失败不中断应用）
  - [x] 6.2.8 添加日志记录（技能数量、工具数量）

- [x] 6.3 编写插件集成测试
  **需求**: NFR-2.3  
  **设计**: 12.2  
  **详情**: 测试完整插件加载流程
  - [x] 6.3.1 测试插件注册（所有服务正确注册到 DI 容器）
  - [x] 6.3.2 测试配置加载（从 IConfiguration）
  - [x] 6.3.3 测试技能加载（使用测试技能目录）
  - [x] 6.3.4 测试工具注册（验证 IFunctionCallback 可从容器解析）
  - [x] 6.3.5 测试钩子注册（验证 IAgentHook 可从容器解析）
  - [x] 6.3.6 测试端到端工作流（从插件加载到工具调用）
  - [x] 6.3.7 测试错误场景（技能目录不存在）
  - [x] 6.3.8 使用 WebApplicationFactory 或类似工具进行集成测试

## 7. 日志和监控

- [x] 7.1 实现日志记录
  **需求**: NFR-2.2, FR-5.3  
  **设计**: 4.3  
  **详情**: 在关键操作点添加日志
  - [x] 7.1.1 在 SkillService 中添加日志（加载开始、成功、失败）
  - [x] 7.1.2 在 AIToolCallbackAdapter 中添加日志（调用、成功、失败）
  - [x] 7.1.3 在 AgentSkillsInstructionHook 中添加日志（注入、跳过）
  - [x] 7.1.4 在 AgentSkillsFunctionHook 中添加日志（注册）
  - [x] 7.1.5 在插件初始化中添加日志
  - [x] 7.1.6 确保日志级别正确（Debug, Info, Warning, Error）
  - [x] 7.1.7 确保日志包含足够的上下文信息（技能名称、数量、错误详情）

- [x] 7.2 验证日志输出
  **需求**: NFR-2.2  
  **设计**: 4.3  
  **详情**: 确保日志包含足够的上下文信息
  - [x] 7.2.1 运行应用并检查日志输出
  - [x] 7.2.2 验证技能加载日志（目录、数量）
  - [x] 7.2.3 验证工具调用日志（工具名称、参数）
  - [x] 7.2.4 验证错误日志（触发错误场景）
  - [x] 7.2.5 验证日志格式一致性

## 8. 文档和示例

- [x] 8.1 更新插件 README
  **需求**: NFR-2.1  
  **设计**: 8  
  **详情**: 提供使用文档
  - [x] 8.1.1 添加功能说明（基于 Agent Skills 规范）
  - [x] 8.1.2 添加配置示例（appsettings.json）
  - [x] 8.1.3 添加技能创建指南（SKILL.md 格式、目录结构）
  - [x] 8.1.4 添加使用示例（如何在 Agent 中使用技能）
  - [x] 8.1.5 添加工具说明（read_skill, read_skill_file, list_skill_directory）
  - [x] 8.1.6 添加故障排除指南
  - [x] 8.1.7 添加 AgentSkillsDotNet 库的链接和说明

- [x] 8.2 创建示例技能
  **需求**: NFR-2.1  
  **设计**: 8  
  **详情**: 提供实用的示例技能
  - [x] 8.2.1 创建 data/skills/ 目录（如果不存在）
  - [x] 8.2.2 创建 pdf-processing 示例（包含 SKILL.md + scripts/ + references/）
  - [x] 8.2.3 创建 data-analysis 示例（包含 Python 脚本）
  - [x] 8.2.4 创建 web-scraping 示例（展示如何使用 assets/）
  - [x] 8.2.5 确保所有示例符合 Agent Skills 规范

- [x] 8.3 创建迁移指南
  **需求**: NFR-2.1  
  **设计**: 8  
  **详情**: 帮助用户从旧版本迁移
  - [x] 8.3.1 编写迁移步骤文档（MIGRATION.md）
  - [x] 8.3.2 列出破坏性变更（如果有）
  - [x] 8.3.3 提供配置迁移示例（旧配置 → 新配置）
  - [x] 8.3.4 说明如何验证迁移成功
  - [x] 8.3.5 提供常见问题解答（FAQ）

## 9. 代码清理和优化

- [x] 9.1 移除或标记旧代码
  **需求**: NFR-2.1  
  **设计**: 13  
  **详情**: 清理不再使用的代码
  - [x] 9.1.1 检查 AgentSkillsConversationHook 是否仍需要（如果为空则删除）
  - [x] 9.1.2 检查 AgentSkillsIntegrationHook 是否被新钩子替代（如果是则删除或标记过时）
  - [x] 9.1.3 更新 Using.cs 文件（移除未使用的 using 语句）
  - [x] 9.1.4 检查是否有重复的代码可以合并
  - [x] 9.1.5 删除未使用的文件和目录

- [x] 9.2 代码审查和重构
  **需求**: NFR-2.1  
  **设计**: 13  
  **详情**: 提高代码质量
  - [x] 9.2.1 检查代码风格一致性（命名、格式、缩进）
  - [x] 9.2.2 运行代码分析工具（Roslyn Analyzers, StyleCop）
  - [x] 9.2.3 修复所有警告和建议
  - [x] 9.2.4 确保所有公共 API 有 XML 文档注释
  - [x] 9.2.5 检查异常处理是否合理
  - [x] 9.2.6 检查资源释放（IDisposable）
  - [x] 9.2.7 进行代码审查（Peer Review）

- [x] 9.3 性能优化
  **需求**: NFR-1.1, NFR-1.2  
  **设计**: 6  
  **详情**: 确保满足性能需求
  - [x] 9.3.1 测量技能加载时间（100个技能应 < 1秒）
  - [x] 9.3.2 测量工具响应时间（应 < 100ms）
  - [x] 9.3.3 使用性能分析工具（BenchmarkDotNet）
  - [x] 9.3.4 优化发现的性能瓶颈
  - [x] 9.3.5 验证缓存机制有效（SkillService 单例）
  - [x] 9.3.6 检查内存使用（避免内存泄漏）

## 10. 测试和验证

- [x] 10.1 运行所有单元测试
  **需求**: NFR-2.3  
  **设计**: 12.1  
  **详情**: 确保所有测试通过
  - [x] 10.1.1 运行测试套件（dotnet test）
  - [x] 10.1.2 确保所有测试通过（0 失败）
  - [x] 10.1.3 生成代码覆盖率报告（dotnet test --collect:"XPlat Code Coverage"）
  - [x] 10.1.4 检查代码覆盖率（目标 > 80%）
  - [x] 10.1.5 为未覆盖的关键代码添加测试
  - [x] 10.1.6 生成覆盖率 HTML 报告（ReportGenerator）

- [x] 10.2 运行集成测试
  **需求**: NFR-2.3  
  **设计**: 12.2  
  **详情**: 测试完整工作流
  - [x] 10.2.1 运行集成测试套件
  - [x] 10.2.2 测试技能加载到工具调用的完整流程
  - [x] 10.2.3 测试错误场景（无效技能、文件不存在、权限不足）
  - [x] 10.2.4 测试配置变更场景（不同配置组合）
  - [x] 10.2.5 测试多租户场景（如果适用）

- [x] 10.3* 运行属性测试
  **需求**: NFR-2.3  
  **设计**: 11  
  **详情**: 验证正确性属性
  - [x] 10.3.1 运行所有属性测试
  - [x] 10.3.2 分析失败的属性测试（查看反例）
  - [x] 10.3.3 修复发现的问题
  - [x] 10.3.4 确保所有属性测试通过
  - [x] 10.3.5 记录属性测试结果

- [ ] 10.4 性能测试
  **需求**: NFR-1.1, NFR-1.2, NFR-1.3  
  **设计**: 6  
  **详情**: 验证性能需求
  - [ ] 10.4.1 测试启动时间（100个技能，目标 < 1秒）
  - [ ] 10.4.2 测试工具响应时间（目标 < 100ms）
  - [ ] 10.4.3 测试内存使用（监控内存增长）
  - [ ] 10.4.4 测试并发访问性能（多个 Agent 同时调用工具）
  - [ ] 10.4.5 测试缓存效果（缓存命中率）
  - [ ] 10.4.6 生成性能报告（BenchmarkDotNet）

- [ ] 10.5 手动测试
  **需求**: NFR-3.1, NFR-3.3  
  **设计**: 7  
  **详情**: 在实际环境中测试
  - [ ] 10.5.1 在 BotSharp 应用中加载插件
  - [ ] 10.5.2 创建测试 Agent 并配置技能目录
  - [ ] 10.5.3 验证 Agent 能看到技能列表（检查指令）
  - [ ] 10.5.4 测试 Agent 调用 read_skill 工具
  - [ ] 10.5.5 测试 Agent 调用 read_skill_file 工具
  - [ ] 10.5.6 测试 Agent 调用 list_skill_directory 工具
  - [ ] 10.5.7 测试与其他插件的兼容性
  - [ ] 10.5.8 测试配置变更（修改 appsettings.json，重启验证）
  - [ ] 10.5.9 测试 Routing 和 Planning Agent（应跳过技能注入）
  - [ ] 10.5.10 记录测试结果和问题

## 11. 安全验证

- [ ] 11.1 验证路径安全
  **需求**: FR-5.1  
  **设计**: 4.1, 11.3  
  **详情**: 确保路径遍历防护有效
  - [ ] 11.1.1 测试包含 ../ 的路径被拒绝
  - [ ] 11.1.2 测试包含 ..\ 的路径被拒绝
  - [ ] 11.1.3 测试访问技能目录外的文件被拒绝
  - [ ] 11.1.4 测试绝对路径被拒绝（如果不在技能目录内）
  - [ ] 11.1.5 验证 AgentSkillsDotNet 库的安全机制
  - [ ] 11.1.6 测试符号链接（symlink）处理
  - [ ] 11.1.7 记录安全测试结果

- [ ] 11.2 验证文件大小限制
  **需求**: FR-5.2  
  **设计**: 4.1, 11.4  
  **详情**: 确保大文件被正确处理
  - [ ] 11.2.1 测试读取超过 MaxOutputSizeBytes 的文件
  - [ ] 11.2.2 验证抛出正确的异常类型
  - [ ] 11.2.3 验证错误消息友好且包含大小信息
  - [ ] 11.2.4 测试边界条件（文件大小 = MaxOutputSizeBytes）
  - [ ] 11.2.5 测试配置变更（修改 MaxOutputSizeBytes）

- [ ] 11.3 审计日志验证
  **需求**: FR-5.3  
  **设计**: 4.3  
  **详情**: 确保关键操作被记录
  - [ ] 11.3.1 验证技能加载操作被记录（目录、数量、时间）
  - [ ] 11.3.2 验证工具调用被记录（工具名称、参数、结果）
  - [ ] 11.3.3 验证错误和异常被记录（错误类型、堆栈跟踪）
  - [ ] 11.3.4 验证安全事件被记录（路径遍历尝试、访问拒绝）
  - [ ] 11.3.5 验证日志包含足够的上下文信息（用户、Agent、时间戳）
  - [ ] 11.3.6 测试日志级别配置（Debug, Info, Warning, Error）

## 12. 发布准备

- [ ] 12.1 版本管理
  **需求**: NFR-2.1  
  **设计**: 13  
  **详情**: 准备发布版本
  - [ ] 12.1.1 更新版本号（BotSharp.Plugin.AgentSkills.csproj）
  - [ ] 12.1.2 更新 CHANGELOG.md（添加新功能、修复、破坏性变更）
  - [ ] 12.1.3 创建 Git 标签（如 v5.3.0）
  - [ ] 12.1.4 更新依赖包版本（如果需要）

- [ ] 12.2 文档最终检查
  **需求**: NFR-2.1  
  **设计**: 8  
  **详情**: 确保文档完整
  - [ ] 12.2.1 检查 README.md 完整性和准确性
  - [ ] 12.2.2 检查代码注释完整性（所有公共 API）
  - [ ] 12.2.3 检查示例技能可用性和正确性
  - [ ] 12.2.4 检查迁移指南准确性
  - [ ] 12.2.5 检查 API 文档（如果生成）
  - [ ] 12.2.6 检查链接有效性

- [ ] 12.3 打包和发布
  **需求**: NFR-2.1  
  **设计**: 13  
  **详情**: 构建和发布插件
  - [ ] 12.3.1 运行完整构建（dotnet build -c Release）
  - [ ] 12.3.2 运行所有测试（dotnet test -c Release）
  - [ ] 12.3.3 构建 NuGet 包（dotnet pack -c Release）
  - [ ] 12.3.4 验证包内容（使用 NuGet Package Explorer）
  - [ ] 12.3.5 验证包依赖关系正确
  - [ ] 12.3.6 发布到 NuGet（如适用）或内部仓库
  - [ ] 12.3.7 创建 GitHub Release（如适用）
  - [ ] 12.3.8 通知用户和团队

## 任务优先级说明

**关键路径任务**（必须按顺序完成）：
1. 项目准备 (1.x)
2. 配置管理 (2.x)
3. 技能服务 (3.x) ← 核心功能
4. 工具适配器 (4.x) ← 核心功能
5. 钩子实现 (5.x) ← 核心功能
6. 插件集成 (6.x) ← 核心功能
7. 测试验证 (10.x)

**并行任务**（可以同时进行）：
- 日志和监控 (7.x) - 在实现核心功能时同步添加
- 文档和示例 (8.x) - 可以在开发过程中逐步完成

**可选任务**（标记 `*`）：
- 属性测试 (3.4, 5.4, 10.3) - 提高质量但非必需

**最终任务**（在所有功能完成后）：
- 代码清理和优化 (9.x)
- 安全验证 (11.x)
- 发布准备 (12.x)

## 估算时间

基于 AgentSkillsDotNet 库的实现，时间估算如下：

| 任务组 | 估算时间 | 说明 |
|--------|----------|------|
| 1. 项目准备 | 2-3 小时 | 环境配置、测试技能创建 |
| 2. 配置管理 | 2 小时 | 简单的配置类更新 |
| 3. 技能服务 | 6-8 小时 | 核心功能，需要仔细实现和测试 |
| 4. 工具适配器 | 4-5 小时 | 适配层实现 |
| 5. 钩子实现 | 4-5 小时 | 两个钩子类 |
| 6. 插件集成 | 4-5 小时 | DI 注册和初始化 |
| 7. 日志和监控 | 2 小时 | 在实现过程中同步添加 |
| 8. 文档和示例 | 4-5 小时 | README、示例技能、迁移指南 |
| 9. 代码清理优化 | 3-4 小时 | 代码审查、重构、性能优化 |
| 10. 测试和验证 | 8-10 小时 | 单元测试、集成测试、手动测试 |
| 11. 安全验证 | 2-3 小时 | 安全测试 |
| 12. 发布准备 | 2 小时 | 版本管理、打包 |

**总计**: 约 43-55 小时

**节省的时间**：由于使用 AgentSkillsDotNet 库，无需实现以下功能：
- 技能发现和扫描（节省 4-6 小时）
- SKILL.md 解析和验证（节省 6-8 小时）
- 路径安全验证（节省 3-4 小时）
- 文件读取和大小限制（节省 3-4 小时）
- 工具生成逻辑（节省 4-6 小时）

**相比从头实现节省**: 约 20-28 小时

## 依赖关系图

```
1. 项目准备
   ↓
2. 配置管理
   ↓
3. 技能服务 ← 依赖 AgentSkillsDotNet 库
   ↓
4. 工具适配器 ← 依赖技能服务
   ↓
5. 钩子实现 ← 依赖技能服务
   ↓
6. 插件集成 ← 依赖所有上述组件
   ↓
7. 日志和监控 ← 贯穿所有组件
   ↓
8. 文档和示例 ← 可并行进行
   ↓
9. 代码清理优化
   ↓
10. 测试和验证
   ↓
11. 安全验证
   ↓
12. 发布准备
```

## 成功标准

完成以下所有标准即可认为重构成功：

### 功能完整性
- [ ] 所有 FR 需求已实现（FR-1.x 到 FR-6.x）
- [ ] 技能加载、工具生成、指令注入功能正常
- [ ] 三个工具（read_skill, read_skill_file, list_skill_directory）可用

### 质量标准
- [ ] 所有单元测试通过（覆盖率 > 80%）
- [ ] 所有集成测试通过
- [ ] 代码无警告（Roslyn Analyzers）
- [ ] 所有公共 API 有 XML 文档注释

### 性能标准
- [ ] 启动时间 < 1秒（100个技能）（NFR-1.1）
- [ ] 工具响应时间 < 100ms（NFR-1.2）
- [ ] 内存使用合理（无内存泄漏）

### 安全标准
- [ ] 路径遍历防护有效（FR-5.1）
- [ ] 文件大小限制有效（FR-5.2）
- [ ] 所有安全测试通过

### 文档完整性
- [ ] README.md 完整且准确
- [ ] 示例技能可用且符合规范
- [ ] 迁移指南清晰
- [ ] API 文档完整

### 兼容性
- [ ] 与 BotSharp 框架无缝集成（NFR-3.3）
- [ ] 完全兼容 Agent Skills 规范（NFR-3.1）
- [ ] 与 AgentSkillsDotNet 库正确集成（NFR-3.2）

### 可维护性
- [ ] 代码结构清晰（NFR-2.1）
- [ ] 日志记录完整（NFR-2.2）
- [ ] 易于扩展（NFR-4.x）

## 风险和缓解措施

### 风险 1: AgentSkillsDotNet 库 API 变更
**影响**: 高  
**概率**: 中  
**缓解**: 
- 锁定库版本
- 创建适配层隔离变更
- 监控库更新

### 风险 2: 性能不达标
**影响**: 中  
**概率**: 低  
**缓解**:
- 早期性能测试
- 使用性能分析工具
- 优化缓存策略

### 风险 3: 与现有代码冲突
**影响**: 中  
**概率**: 中  
**缓解**:
- 充分的集成测试
- 代码审查
- 渐进式迁移

### 风险 4: 文档不完整
**影响**: 低  
**概率**: 中  
**缓解**:
- 在开发过程中同步更新文档
- 文档审查检查清单
- 用户反馈收集

## 下一步行动

1. **审查本任务列表**：确认所有任务合理且完整
2. **设置开发环境**：完成任务 1.1-1.3
3. **开始核心开发**：按顺序执行任务 2.x-6.x
4. **持续测试**：在开发过程中运行测试
5. **文档同步**：在开发过程中更新文档
6. **最终验证**：完成任务 10.x-11.x
7. **准备发布**：完成任务 12.x

**建议开始时间**: 准备就绪后立即开始  
**预计完成时间**: 6-7 个工作日（全职开发）

---

**注意事项**：
- 本任务列表基于 AgentSkillsDotNet 库，大幅简化了实现复杂度
- 所有任务都有明确的需求追溯和设计参考
- 可选任务（标记 `*`）可根据时间和资源决定是否执行
- 建议使用项目管理工具（如 GitHub Projects, Jira）跟踪任务进度
