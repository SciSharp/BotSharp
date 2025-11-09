# ITaskPlanner Interface Guide / ITaskPlanner 接口指南

## Table of Contents / 目录
1. [Interface Contract / 接口契约](#interface-contract--接口契约)
2. [Implementation Analysis / 实现分析](#implementation-analysis--实现分析)
3. [Available Implementations / 可用实现](#available-implementations--可用实现)
4. [Agent Configuration / Agent 配置](#agent-configuration--agent-配置)
5. [Complete Usage Workflow / 完整使用流程](#complete-usage-workflow--完整使用流程)
6. [Architecture & Integration / 架构与集成](#architecture--integration--架构与集成)

---

## Interface Contract / 接口契约

### Location / 位置
The `ITaskPlanner` interface is defined in:
`ITaskPlanner` 接口定义在：
```
src/Infrastructure/BotSharp.Abstraction/Planning/ITaskPlanner.cs
```

### Interface Definition / 接口定义

```csharp
/// <summary>
/// Planning process for Task Agent
/// https://www.promptingguide.ai/techniques/cot
/// </summary>
public interface ITaskPlanner
{
    string Name => "Unamed Task Planner";
    
    Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs);
    
    Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs);
    
    Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs);
    
    List<RoleDialogModel> BeforeHandleContext(FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
        => dialogs;
    
    bool AfterHandleContext(List<RoleDialogModel> dialogs, List<RoleDialogModel> taskAgentDialogs)
        => true;
    
    int MaxLoopCount => 5;
}
```

### Core Methods / 核心方法

#### 1. GetNextInstruction
**Purpose / 用途**: Determines the next action to take based on conversation history and user goals.
根据对话历史和用户目标确定下一步要采取的行动。

**Parameters / 参数**:
- `Agent router`: The routing agent that manages task flow / 管理任务流的路由代理
- `string messageId`: Unique identifier for the current message / 当前消息的唯一标识符
- `List<RoleDialogModel> dialogs`: Complete conversation history / 完整的对话历史

**Returns / 返回值**: 
`FunctionCallFromLlm` - A structured instruction containing:
一个包含以下内容的结构化指令：
- `Function`: The function/tool to execute / 要执行的函数/工具
- `Arguments`: Parameters for the function / 函数的参数
- `Question`: The question or task description / 问题或任务描述
- `AgentName`: The target agent to route to / 要路由到的目标代理
- `NextActionReason`: Explanation for the next action / 下一步行动的解释

#### 2. AgentExecuting
**Purpose / 用途**: Hook called before an agent starts executing an instruction.
在代理开始执行指令之前调用的钩子。

**Use Case / 使用场景**: Prepare message context, modify parameters, or perform validation.
准备消息上下文、修改参数或执行验证。

#### 3. AgentExecuted
**Purpose / 用途**: Hook called after an agent completes execution.
在代理完成执行后调用的钩子。

**Use Case / 使用场景**: Clean up context, decide whether to continue the loop, or handle completion.
清理上下文、决定是否继续循环或处理完成。

#### 4. BeforeHandleContext & AfterHandleContext
**Purpose / 用途**: Manage dialog context transformation between planning and execution phases.
管理规划和执行阶段之间的对话上下文转换。

#### 5. MaxLoopCount
**Purpose / 用途**: Maximum number of planning-execution iterations to prevent infinite loops.
规划-执行迭代的最大次数，以防止无限循环。

---

## Implementation Analysis / 实现分析

### How ITaskPlanner Works / ITaskPlanner 如何工作

The `ITaskPlanner` interface follows a **Chain-of-Thought (CoT)** reasoning pattern to decompose complex user goals into executable steps:

`ITaskPlanner` 接口遵循**思维链 (CoT)** 推理模式，将复杂的用户目标分解为可执行的步骤：

```
User Input (用户输入)
    ↓
GetNextInstruction (获取下一指令)
    ↓
LLM Reasoning (LLM 推理)
    ↓
FunctionCallFromLlm (函数调用)
    ↓
AgentExecuting (代理执行前)
    ↓
Execute Task Agent (执行任务代理)
    ↓
AgentExecuted (代理执行后)
    ↓
Loop or Complete (循环或完成)
```

### Key Concepts / 关键概念

#### a) Receiving High-Level Goals / 接收高级目标
The planner receives user goals through the `dialogs` parameter, which contains the complete conversation history including:
规划器通过 `dialogs` 参数接收用户目标，其中包含完整的对话历史，包括：

- User messages (用户消息)
- Previous agent responses (先前的代理响应)
- Function execution results (函数执行结果)

#### b) LLM Interaction / LLM 交互
The planner interacts with LLMs by:
规划器通过以下方式与 LLM 交互：

1. **Creating specialized prompts / 创建专用提示词** using agent templates (e.g., `two_stage.next.liquid`)
2. **Calling chat completion / 调用聊天完成** with structured system instructions
3. **Parsing LLM responses / 解析 LLM 响应** into `FunctionCallFromLlm` objects

Example code from `TwoStageTaskPlanner`:
来自 `TwoStageTaskPlanner` 的示例代码：

```csharp
var completion = CompletionProvider.GetChatCompletion(_services,
    provider: router?.LlmConfig?.Provider,
    model: router?.LlmConfig?.Model);

var response = await completion.GetChatCompletions(router, dialogs);
inst = response.Content.JsonContent<FunctionCallFromLlm>();
```

#### c) Plan Data Structure / 计划数据结构
The plan is represented as a `FunctionCallFromLlm` object, which is essentially a **function call instruction** containing:
计划表示为 `FunctionCallFromLlm` 对象，本质上是一个**函数调用指令**，包含：

```csharp
public class FunctionCallFromLlm : RoutingArgs
{
    public string? Function { get; set; }        // Function/tool name to execute
    public JsonDocument? Arguments { get; set; }  // Function parameters
    public string? Question { get; set; }         // Task description
    public string? Summary { get; set; }          // Conversation summary
    public bool ExecutingDirectly { get; set; }   // Execution flag
    public string? AgentName { get; set; }        // Target agent
    public string? NextActionReason { get; set; } // Reasoning explanation
}
```

This is **not a dependency graph** but rather a **sequential instruction** that tells the system:
这**不是依赖关系图**，而是一个**顺序指令**，告诉系统：
- Which function to call / 调用哪个函数
- What arguments to pass / 传递什么参数
- Which agent should handle it / 哪个代理应该处理它
- Why this action was chosen / 为什么选择这个行动

---

## Available Implementations / 可用实现

BotSharp provides three built-in `ITaskPlanner` implementations:
BotSharp 提供三种内置的 `ITaskPlanner` 实现：

### 1. SequentialPlanner (顺序规划器)

**Location / 位置**: `src/Plugins/BotSharp.Plugin.Planner/Sequential/SequentialPlanner.cs`

**Description / 描述**: 
A planner that executes tasks in a predefined order specified by the user. It follows a linear execution model where tasks are completed one after another.
按照用户指定的预定义顺序执行任务的规划器。它遵循线性执行模型，任务依次完成。

**Key Features / 关键特性**:
- Linear task execution / 线性任务执行
- User-defined step order / 用户定义的步骤顺序
- Tracks remaining steps / 跟踪剩余步骤
- Max loop count: 100 / 最大循环次数：100

**Best For / 适用于**:
- Multi-step workflows with clear ordering / 具有明确顺序的多步骤工作流
- Tasks where steps must be completed sequentially / 步骤必须按顺序完成的任务
- Data processing pipelines / 数据处理管道

**Example Code Flow / 示例代码流程**:
```csharp
public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
{
    // 1. Get decomposed steps
    var decomposation = await GetDecomposedStepAsync(router, messageId, dialogs);
    
    // 2. Check if more steps remain
    if (decomposation.TotalRemainingSteps > 0 && _lastInst != null)
    {
        _lastInst.NextActionReason = $"Having {decomposation.TotalRemainingSteps} steps left.";
        return _lastInst;
    }
    
    // 3. Get next step prompt from template
    var next = GetNextStepPrompt(router);
    
    // 4. Use LLM to determine next action
    var completion = CompletionProvider.GetChatCompletion(_services);
    var response = await completion.GetChatCompletions(router, dialogs);
    var inst = response.Content.JsonContent<FunctionCallFromLlm>();
    
    return inst;
}
```

### 2. TwoStageTaskPlanner (两阶段任务规划器)

**Location / 位置**: `src/Plugins/BotSharp.Plugin.Planner/TwoStaging/TwoStageTaskPlanner.cs`

**Description / 描述**:
A sophisticated planner that breaks complex tasks into two stages:
一个复杂的规划器，将复杂任务分为两个阶段：
1. **Primary Stage / 主要阶段**: Creates high-level overview / 创建高级概述
2. **Secondary Stage / 次要阶段**: Elaborates with specific actions / 用具体行动详细说明

**Key Features / 关键特性**:
- Two-phase planning approach / 两阶段规划方法
- Knowledge base integration / 知识库集成
- Dictionary term verification / 字典术语验证
- Dynamic task breakdown / 动态任务分解

**Best For / 适用于**:
- Complex business requirements / 复杂的业务需求
- SQL generation tasks / SQL 生成任务
- Tasks requiring knowledge lookup / 需要知识查询的任务
- Multi-entity queries / 多实体查询

**Agent ID / 代理 ID**: `282a7128-69a1-44b0-878c-a9159b88f3b9`

**Available Functions / 可用函数**:
1. `plan_primary_stage`: Generate high-level plan / 生成高级计划
2. `plan_secondary_stage`: Detail specific steps / 详细说明具体步骤
3. `plan_summary`: Summarize final plan / 总结最终计划
4. `verify_dictionary_term`: Lookup terms in knowledge base / 在知识库中查找术语

### 3. SqlGenerationPlanner (SQL生成规划器)

**Location / 位置**: `src/Plugins/BotSharp.Plugin.Planner/SqlGeneration/SqlGenerationPlanner.cs`

**Description / 描述**:
Specialized planner for generating and reviewing SQL statements from natural language requirements.
专门用于从自然语言需求生成和审查 SQL 语句的规划器。

**Key Features / 关键特性**:
- SQL-specific planning / SQL 特定规划
- Query generation / 查询生成
- Query validation / 查询验证
- Database schema awareness / 数据库架构感知

**Best For / 适用于**:
- Natural language to SQL conversion / 自然语言到 SQL 的转换
- Database query optimization / 数据库查询优化
- SQL template generation / SQL 模板生成

**Agent ID / 代理 ID**: `da7aad2c-8112-48a2-ab7b-1f87da524741`

---

## Agent Configuration / Agent 配置

### Example: Two-Stage Planner Configuration / 示例：两阶段规划器配置

**File / 文件**: `src/Plugins/BotSharp.Plugin.Planner/data/agents/282a7128-69a1-44b0-878c-a9159b88f3b9/agent.json`

```json
{
  "id": "282a7128-69a1-44b0-878c-a9159b88f3b9",
  "name": "Two-Stage-Planner",
  "description": "Plan feasible steps for complex user task request, including generating sql query",
  "type": "planning",
  "createdDateTime": "2023-08-27T10:39:00Z",
  "updatedDateTime": "2023-08-27T14:39:00Z",
  "iconUrl": "https://e7.pngegg.com/pngimages/775/350/png-clipart-action-plan-computer-icons-plan-miscellaneous-text-thumbnail.png",
  "disabled": false,
  "isPublic": true,
  "profiles": [ "planning" ],
  "mergeUtility": true,
  "utilities": [],
  "llmConfig": {
    "provider": "openai",
    "model": "gpt-4o-2024-11-20",
    "max_recursion_depth": 10
  }
}
```

### Key Configuration Fields / 关键配置字段

#### type: "planning"
Identifies this agent as a planning agent. This is crucial for the routing system.
将此代理标识为规划代理。这对路由系统至关重要。

#### profiles: ["planning"]
Defines the agent profile. Router agents must have matching profiles to include this agent in routing.
定义代理配置文件。路由器代理必须具有匹配的配置文件才能将此代理包含在路由中。

#### llmConfig
Specifies the LLM provider and model:
指定 LLM 提供程序和模型：
- `provider`: LLM provider (e.g., "openai", "azure-openai") / LLM 提供程序
- `model`: Specific model name / 特定模型名称
- `max_recursion_depth`: Maximum planning depth / 最大规划深度

#### mergeUtility
When `true`, the agent can access utility functions defined in other agents.
当为 `true` 时，代理可以访问其他代理中定义的实用函数。

### Specifying Available Tools / 指定可用工具

Tools (functions) are defined in the `functions/` directory of the agent:
工具（函数）在代理的 `functions/` 目录中定义：

```
agent/
├── agent.json
├── instructions/
│   └── instruction.liquid
├── templates/
│   ├── two_stage.1st.plan.liquid
│   ├── two_stage.2nd.plan.liquid
│   └── two_stage.next.liquid
└── functions/
    ├── plan_primary_stage.json
    ├── plan_secondary_stage.json
    └── plan_summary.json
```

**Example Function Definition / 示例函数定义**:
```json
{
  "name": "plan_primary_stage",
  "description": "Plan the high level steps to finish the task",
  "parameters": {
    "type": "object",
    "properties": {
      "requirement_detail": {
        "type": "string",
        "description": "User original requirements in detail"
      },
      "questions": {
        "type": "array",
        "description": "Break down user requirements",
        "items": {
          "type": "string"
        }
      }
    },
    "required": ["requirement_detail", "questions"]
  }
}
```

---

## Complete Usage Workflow / 完整使用流程

### Scenario: Multi-Step Data Query / 场景：多步骤数据查询

Let's walk through a complete example of how a planning agent handles a complex user request.
让我们完整地看一个规划代理如何处理复杂用户请求的示例。

#### User Request / 用户请求
"Find all customers who placed orders in the last month and calculate their total spending, then send them a promotional email if they spent more than $500."

"查找上个月下订单的所有客户并计算他们的总支出，然后如果他们的支出超过 500 美元，就给他们发送促销电子邮件。"

### Step-by-Step Flow / 分步流程

#### 1. Router Initialization / 路由器初始化

**File / 文件**: `src/Infrastructure/BotSharp.Core/Routing/RoutingService.InstructLoop.cs`

```csharp
public async Task<RoleDialogModel> InstructLoop(Agent agent, RoleDialogModel message, List<RoleDialogModel> dialogs)
{
    _router = agent;
    var reasoner = GetReasoner(_router);  // Gets the configured planner
    
    // Add user message to dialog history
    dialogs.Add(message);
    
    // Get first instruction from planner
    var inst = await reasoner.GetNextInstruction(_router, message.MessageId, dialogs);
    
    int loopCount = 1;
    while (true)
    {
        // Execute instruction...
    }
}
```

The router selects which planner to use based on `routingRules` in the agent configuration:
路由器根据代理配置中的 `routingRules` 选择使用哪个规划器：

```json
{
  "routingRules": [
    {
      "type": "reasoner",
      "field": "Two-Stage-Planner"
    }
  ]
}
```

#### 2. First Planning Phase / 第一次规划阶段

**TwoStageTaskPlanner.GetNextInstruction** is called:
调用 **TwoStageTaskPlanner.GetNextInstruction**：

```csharp
public async Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId, List<RoleDialogModel> dialogs)
{
    // Create specialized prompt
    var nextStepPrompt = await GetNextStepPrompt(router);
    
    // Call LLM with conversation context
    var completion = CompletionProvider.GetChatCompletion(_services);
    dialogs = new List<RoleDialogModel>
    {
        new RoleDialogModel(AgentRole.User, nextStepPrompt)
        {
            FunctionName = nameof(TwoStageTaskPlanner),
            MessageId = messageId
        }
    };
    
    var response = await completion.GetChatCompletions(router, dialogs);
    inst = response.Content.JsonContent<FunctionCallFromLlm>();
    
    return inst;
}
```

**LLM Response / LLM 响应** (First Instruction):
```json
{
  "function": "plan_primary_stage",
  "args": {
    "requirement_detail": "Find customers with orders in last month, calculate spending, send promo email if >$500",
    "questions": [
      "What are the customers who placed orders in the last month?",
      "What is the total spending for each customer?",
      "Which customers spent more than $500?"
    ],
    "entities": [
      {"type": "time_period", "value": "last month"},
      {"type": "threshold_amount", "value": "$500"}
    ]
  },
  "agent_name": "Database-Agent",
  "next_action_reason": "Need to query customer orders from database"
}
```

#### 3. Agent Execution Phase / 代理执行阶段

**AgentExecuting Hook**:
```csharp
public async Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
{
    // Prepare message context
    message.FunctionName = inst.Function;
    message.FunctionArgs = inst.Arguments == null ? "{}" : JsonSerializer.Serialize(inst.Arguments);
    return true;
}
```

**Execution**:
The routing service routes to the Database Agent, which executes the query:
路由服务路由到数据库代理，执行查询：

```sql
SELECT 
    c.customer_id,
    c.email,
    SUM(o.amount) as total_spending
FROM customers c
JOIN orders o ON c.customer_id = o.customer_id
WHERE o.order_date >= DATE_SUB(NOW(), INTERVAL 1 MONTH)
GROUP BY c.customer_id, c.email
```

**Result / 结果**:
```json
[
  {"customer_id": 101, "email": "john@example.com", "total_spending": 650},
  {"customer_id": 102, "email": "jane@example.com", "total_spending": 320},
  {"customer_id": 103, "email": "bob@example.com", "total_spending": 890}
]
```

#### 4. Second Planning Phase / 第二次规划阶段

**AgentExecuted Hook**:
```csharp
public async Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message, List<RoleDialogModel> dialogs)
{
    if (message.StopCompletion)
    {
        context.Empty(reason: $"Agent queue is cleared");
        return false;
    }
    
    // Continue to next iteration
    return true;
}
```

**GetNextInstruction** is called again with updated dialog history:
再次调用 **GetNextInstruction**，使用更新的对话历史：

**LLM Response / LLM 响应** (Second Instruction):
```json
{
  "function": "send_email",
  "args": {
    "recipients": ["john@example.com", "bob@example.com"],
    "subject": "Special Promotion for Valued Customers",
    "template": "promotional_email",
    "filter_condition": "total_spending > 500"
  },
  "agent_name": "Email-Agent",
  "next_action_reason": "Send promotional emails to high-value customers"
}
```

#### 5. Final Execution / 最终执行

The Email Agent sends promotional emails to customers who spent >$500.
电子邮件代理向消费超过 $500 的客户发送促销电子邮件。

#### 6. Loop Termination / 循环终止

The loop continues until:
循环继续，直到：
- `loopCount >= reasoner.MaxLoopCount` (maximum iterations reached / 达到最大迭代次数)
- `_context.IsEmpty` (no more agents in queue / 队列中没有更多代理)
- `response.StopCompletion` (task completed / 任务完成)

```csharp
if (loopCount >= reasoner.MaxLoopCount || _context.IsEmpty || response.StopCompletion)
{
    break;
}
```

---

## Architecture & Integration / 架构与集成

### Relationship Between ITaskPlanner and IRoutingReasoner
### ITaskPlanner 与 IRoutingReasoner 的关系

Both interfaces share identical method signatures:
两个接口共享相同的方法签名：

```csharp
public interface IRoutingReasoner
{
    string Name => "Unnamed Reasoner";
    int MaxLoopCount => 5;
    Task<FunctionCallFromLlm> GetNextInstruction(...);
    Task<bool> AgentExecuting(...);
    Task<bool> AgentExecuted(...);
    List<RoleDialogModel> BeforeHandleContext(...);
    bool AfterHandleContext(...);
}
```

**Key Insight / 关键见解**: 
`ITaskPlanner` is essentially a specialized type of `IRoutingReasoner` focused on **task decomposition and planning**, while other reasoners (like `NaiveReasoner`) handle simpler routing logic.

`ITaskPlanner` 本质上是一种专门的 `IRoutingReasoner` 类型，专注于**任务分解和规划**，而其他推理器（如 `NaiveReasoner`）处理更简单的路由逻辑。

### Plugin Registration / 插件注册

**File / 文件**: `src/Plugins/BotSharp.Plugin.Planner/PlannerPlugin.cs`

```csharp
public class PlannerPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // Register all planner implementations
        services.AddScoped<ITaskPlanner, SequentialPlanner>();
        services.AddScoped<ITaskPlanner, TwoStageTaskPlanner>();
        services.AddScoped<ITaskPlanner, SqlGenerationPlanner>();
        
        // Register hooks for lifecycle management
        services.AddScoped<IAgentHook, SqlPlannerAgentHook>();
        services.AddScoped<IAgentUtilityHook, TwoStagingPlannerUtilityHook>();
    }
}
```

### Integration Points / 集成点

1. **Routing Service / 路由服务**
   - Entry point for all conversations / 所有对话的入口点
   - Manages agent stack / 管理代理堆栈
   - Executes planning loop / 执行规划循环

2. **Agent Service / 代理服务**
   - Loads agent configurations / 加载代理配置
   - Manages agent lifecycle / 管理代理生命周期
   - Provides agent metadata / 提供代理元数据

3. **LLM Providers / LLM 提供程序**
   - OpenAI, Azure OpenAI, Anthropic, etc. / OpenAI、Azure OpenAI、Anthropic 等
   - Chat completion / 聊天完成
   - Function calling / 函数调用

4. **Function Executor / 函数执行器**
   - Executes functions defined in agent configuration / 执行代理配置中定义的函数
   - Handles parameter binding / 处理参数绑定
   - Returns structured results / 返回结构化结果

### System Flow Diagram / 系统流程图

```
┌─────────────────────────────────────────────────────────────┐
│                      User Input                             │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              Routing Service                                │
│              (InstructLoop)                                 │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│          Get Reasoner (ITaskPlanner)                        │
│          - SequentialPlanner                                │
│          - TwoStageTaskPlanner                              │
│          - SqlGenerationPlanner                             │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│        GetNextInstruction                                   │
│        - Load agent templates                               │
│        - Create prompt                                      │
│        - Call LLM                                           │
│        - Parse response to FunctionCallFromLlm              │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│        AgentExecuting (Pre-execution hook)                  │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│          Function Executor                                  │
│          - Route to Task Agent                              │
│          - Execute function                                 │
│          - Return result                                    │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│        AgentExecuted (Post-execution hook)                  │
│        - Check completion                                   │
│        - Decide next action                                 │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
                  Loop or Exit
```

---

## Additional Resources / 其他资源

### Official Documentation / 官方文档
- Main Documentation: https://botsharp.readthedocs.io
- New Documentation: https://botsharp.verdure-hiro.cn
- GitHub Repository: https://github.com/SciSharp/BotSharp
- Chain-of-Thought Guide: https://www.promptingguide.ai/techniques/cot

### Related Documentation Files / 相关文档文件
- Architecture: `docs/architecture/routing.md`
- Agent Introduction: `docs/agent/intro.md`
- Agent Router: `docs/agent/router.md`

### Example Projects / 示例项目
- PizzaBot: `tests/BotSharp.Plugin.PizzaBot/`
  - Demonstrates multi-agent routing / 演示多代理路由
  - Shows planning integration / 显示规划集成
  - Example agent configurations / 示例代理配置

### Key Template Files / 关键模板文件
- Two-Stage Primary Planning: `src/Plugins/BotSharp.Plugin.Planner/data/agents/282a7128-69a1-44b0-878c-a9159b88f3b9/templates/two_stage.1st.plan.liquid`
- Two-Stage Next Action: `src/Plugins/BotSharp.Plugin.Planner/data/agents/282a7128-69a1-44b0-878c-a9159b88f3b9/templates/two_stage.next.liquid`
- Sequential Planner Decomposition: Agent-specific templates in instruction files

---

## Summary / 总结

The `ITaskPlanner` interface in BotSharp provides a powerful abstraction for implementing AI-driven task planning and decomposition. By leveraging Chain-of-Thought reasoning with LLMs, planners can break down complex user goals into executable steps, coordinate multiple agents, and adapt dynamically based on execution results.

BotSharp 中的 `ITaskPlanner` 接口为实现 AI 驱动的任务规划和分解提供了强大的抽象。通过利用 LLM 的思维链推理，规划器可以将复杂的用户目标分解为可执行的步骤，协调多个代理，并根据执行结果动态调整。

**Key Takeaways / 要点**:
1. ITaskPlanner follows CoT (Chain-of-Thought) reasoning / ITaskPlanner 遵循 CoT（思维链）推理
2. Plans are represented as FunctionCallFromLlm objects / 计划表示为 FunctionCallFromLlm 对象
3. Three implementations available: Sequential, TwoStage, and SQL / 三种可用实现：顺序、两阶段和 SQL
4. Integrates seamlessly with routing and agent systems / 与路由和代理系统无缝集成
5. Configurable via agent.json and template files / 通过 agent.json 和模板文件可配置

For further exploration, refer to the source code and example projects in the BotSharp repository.
欲了解更多信息，请参阅 BotSharp 存储库中的源代码和示例项目。
