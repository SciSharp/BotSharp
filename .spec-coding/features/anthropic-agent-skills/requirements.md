# Anthropic Agent Skills 需求文档 (Requirements)

## 1. 概述 (Overview)
本功能旨在为 BotSharp 引入 Anthropic 的 "Agent Skills" 标准支持。通过实现一个新的插件 `BotSharp.Plugin.AgentSkills`，允许 Agent 采用“渐进式披露”（Progressive Disclosure）模式进行交互。与传统的一次性加载所有工具不同，Agent 将首先感知可用技能的摘要，根据需要“加载”特定技能的详细指令，并通过通用接口“执行”技能中定义的脚本。

## 2. 需求列表 (Requirements)

### 2.1 技能索引与感知 (Indexing & Awareness)
**用户故事**: 作为 BotSharp 管理员，我希望系统能自动发现指定目录下的技能，以便 Agent 在初始状态下知道有哪些能力可用，而无需预加载所有繁重的指令。

*   **REQ-001**: 当 Agent 初始化或系统启动时，组件 **必须** 扫描配置的技能根目录。
*   **REQ-002**: 当发现从属目录中包含 `SKILL.md` 时，系统 **必须** 解析其 YAML Frontmatter 以提取 `name` 和 `description`。
*   **REQ-003**: 如果 YAML 解析成功，系统 **必须** 将格式化后的技能列表（包含名称和描述）注入到 Agent 的 System Prompt（或可用的 Context Window）中。
*   **REQ-004**: 如果 `SKILL.md` 格式无效或元数据缺失，系统 **应该** 记录警告日志并跳过该技能，即不中断整体启动流程。

### 2.2 技能加载 (Skill Loading)
**用户故事**: 作为 LLM Agent，我希望在判断某个技能对当前任务有用时能够动态加载其详细指令，以便获取执行任务所需的标准作业程序（SOP）。

*   **REQ-005**: 系统 **必须** 向 LLM 暴露一个名为 `load_skill`（或语义等效）的 Function Tool。
*   **REQ-006**: 当 `load_skill` 被调用且 `skill_name` 有效时，系统 **必须** 读取对应 `SKILL.md` 的 Markdown 正文（Body）。
*   **REQ-007**: 当获取到 Markdown 正文后，系统 **必须** 将其作为消息（System 或 User 角色）追加到当前的对话上下文中，使其对后续推理可见。
*   **REQ-008**: 如果请求的 `skill_name` 不存在，工具调用 **必须** 返回明确的错误提示给 Agent。

### 2.3 技能脚本执行 (Skill Invocation)
**用户故事**: 作为 LLM Agent，我希望能通过一个通用的执行接口运行技能包中定义的脚本，以便按照 SOP 完成具体的操作。

*   **REQ-009**: 系统 **必须** 向 LLM 暴露一个名为 `run_skill_script` 的 Function Tool，接受 `skill_name`, `script_file`, `args` 等参数。
*   **REQ-010**: 当 `run_skill_script` 被调用时，系统 **必须** 验证请求的脚本文件是否存在于该技能的 `scripts/` 子目录下，防止路径遍历攻击。
*   **REQ-011**: 如果脚本是 Python 文件（`.py`），系统 **必须** 调用宿主环境的 Python 解释器执行该脚本，并将 `args` 传递给进程。
*   **REQ-012**: 当脚本执行完成，系统 **必须** 捕获标准输出（Stdout）作为工具的成功返回值。
*   **REQ-013**: 如果脚本执行失败（退出码非0或抛出异常），系统 **必须** 捕获错误输出（Stderr）并将其包装为错误信息返回给 Agent。

### 2.4 配置与扩展性 (Configuration & Extensibility)
**用户故事**: 作为开发者，我希望能够灵活配置技能库的位置，并以插件形式集成此功能。

*   **REQ-014**: 系统 **必须** 利用 BotSharp 的配置机制（如 `appsettings.json`）读取技能库的根路径（例如 `AgentSkills:DataDir`）。
*   **REQ-015**: 如果未提供配置，系统 **应该** 默认使用应用工作目录下的 `skills` 文件夹。
*   **REQ-016**: 所有功能 **必须** 封装在 `BotSharp.Plugin.AgentSkills` 项目中，通过实现 `IBotSharpPlugin` 接口进行服务注册。

## 3. 验收标准示例 (Acceptance Criteria Example)
*   **Case 1: 发现技能**: 在 `skills/pdf-processing/SKILL.md` 存在的情况下，启动 Agent，System Prompt 中应包含 "pdf-processing" 及其描述。
*   **Case 2: 动态加载**: 对 Agent 说 "我要处理 PDF"，Agent 调用 `load_skill("pdf-processing")`，随后的 Prompt 中包含了 PDF 处理的具体步骤。
*   **Case 3: 脚本执行**: Agent 调用 `run_skill_script("pdf-processing", "analyze.py", ...)`，系统成功执行本地 Python 脚本并返回结果字符串。
