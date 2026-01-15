# Anthropic Agent Skills 需求文档

## 1. 概述
在 BotSharp 中实现 Antrhopic 推出的 "Agent Skills" 标准。该标准采用“文件系统中心化”设计和“渐进式披露”范式。Agent 不会在初始阶段加载所有工具，而是根据任务需求动态发现、加载并执行能力。

## 2. 核心组件 (基于文件系统的技能单元)
每个 Skill 是一个独立的目录，包含以下核心文件：
*   **`SKILL.md`**: 核心定义文件。
    *   **YAML Frontmatter**: 包含 `name` (唯一标识) 和 `description` (用于发现)。
    *   **Markdown Body**: 包含详细的 SOP (标准作业程序/指令)。这部分仅在加载后对 LLM 可见。
*   **`scripts/`**: 包含可执行逻辑的代码文件 (如 `.py`, `.sh`)。
*   **`resources/`**: (可选) 静态资源文件。

## 3. 交互生命周期 (The Progressive Paradigm)
系统需支持以下四个关键阶段：

1.  **索引 (Indexing)**:
    *   系统需扫描指定目录下所有的 Skill 文件夹。
    *   解析每个 Skill 的 `SKILL.md` 中的 YAML 头信息。

2.  **感知 (Awareness)**:
    *   将所有已发现 Skill 的 `name` 和 `description` 注入到 Agent 的初始 System Prompt 中。
    *   目的：让 LLM 知道有哪些技能“可用”，但不知道具体的指令细节。

3.  **加载 (Loading)**:
    *   **机制**: 提供内置工具 `load_skill(skill_name)` (或类似命名)。
    *   **流程**: 当 LLM 决定使用某技能并调用此工具时，系统读取对应 `SKILL.md` 的 Markdown 正文。
    *   **结果**: 将正文内容追加到当前的对话上下文中 (Context)，使 LLM 获得执行该任务的详细 SOP。

4.  **调用 (Invocation)**:
    *   **机制**: 提供内置工具 `run_skill_script(skill_name, script_file, args)` (方案A)。
    *   **流程**: LLM 根据 SOP 中的指示，构造参数调用此工具。
    *   **执行**: 系统在宿主环境中执行对应的 `scripts/` 下的代码文件 (支持 Python 等)，并将标准输出作为结果返回给 LLM。

## 4. 技术约束
*   **插件化**: 实现为 `BotSharp.Plugin.AgentSkills`。
*   **脚本执行**: 需复用或扩展现有的 Python 执行能力，支持“预定义代码”的运行，而非仅支持 REPL 模式。
*   **配置**: 需支持配置 Skill 库的根路径。
