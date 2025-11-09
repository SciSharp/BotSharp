# Planning

Planning is a crucial capability in BotSharp that enables AI agents to decompose complex user requests into executable steps. The planning system uses the **ITaskPlanner** interface to implement various reasoning strategies.

## ITaskPlanner Interface

The `ITaskPlanner` interface is defined in `BotSharp.Abstraction.Planning` and provides the contract for implementing task planning strategies. It follows the **Chain-of-Thought (CoT)** reasoning pattern to break down complex goals into manageable tasks.

### Core Methods

- **GetNextInstruction**: Determines the next action based on conversation history and user goals
- **AgentExecuting**: Hook called before agent execution (preparation phase)
- **AgentExecuted**: Hook called after agent execution (cleanup phase)
- **BeforeHandleContext** / **AfterHandleContext**: Manage dialog context transformation
- **MaxLoopCount**: Maximum planning iterations to prevent infinite loops

## Available Planners

BotSharp includes three built-in planning implementations:

### 1. SequentialPlanner

A planner that executes tasks in a predefined order specified by the user. Best for multi-step workflows where tasks must be completed sequentially.

**Agent ID**: `3e75e818-a139-48a8-9e22-4662548c13a3`

**Use Cases**:
- Data processing pipelines
- Step-by-step workflows
- Ordered task execution

### 2. TwoStageTaskPlanner

A sophisticated planner that breaks complex tasks into two stages:
1. **Primary Stage**: Creates high-level overview
2. **Secondary Stage**: Elaborates with specific actions

**Agent ID**: `282a7128-69a1-44b0-878c-a9159b88f3b9`

**Use Cases**:
- Complex business requirements
- SQL generation tasks
- Tasks requiring knowledge lookup
- Multi-entity queries

**Available Functions**:
- `plan_primary_stage`: Generate high-level plan
- `plan_secondary_stage`: Detail specific steps
- `plan_summary`: Summarize final plan
- `verify_dictionary_term`: Lookup terms in knowledge base

### 3. SqlGenerationPlanner

Specialized planner for generating and reviewing SQL statements from natural language requirements.

**Agent ID**: `da7aad2c-8112-48a2-ab7b-1f87da524741`

**Use Cases**:
- Natural language to SQL conversion
- Database query optimization
- SQL template generation

## How Planning Works

The planning process follows this flow:

```
User Input
    ↓
GetNextInstruction (LLM determines next action)
    ↓
AgentExecuting (Pre-execution hook)
    ↓
Execute Task Agent (Function/tool execution)
    ↓
AgentExecuted (Post-execution hook)
    ↓
Loop or Complete
```

### Integration with Routing

Planning agents integrate with the routing system through the `IRoutingReasoner` interface. The router selects which planner to use based on the agent's `routingRules` configuration:

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

## Configuration

### Agent Configuration Example

```json
{
  "id": "282a7128-69a1-44b0-878c-a9159b88f3b9",
  "name": "Two-Stage-Planner",
  "description": "Plan feasible steps for complex user task request",
  "type": "planning",
  "profiles": ["planning"],
  "llmConfig": {
    "provider": "openai",
    "model": "gpt-4o-2024-11-20",
    "max_recursion_depth": 10
  }
}
```

### Key Configuration Fields

- **type**: Must be "planning" for planning agents
- **profiles**: Defines agent profile for routing matching
- **llmConfig**: Specifies LLM provider and model
- **mergeUtility**: When true, agent can access utility functions from other agents

## Function Definition

Planning agents can define custom functions in the `functions/` directory:

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
      }
    },
    "required": ["requirement_detail"]
  }
}
```

## Example Usage

Here's a complete example of how a planning agent handles a complex request:

**User Request**: "Find customers who ordered last month and send them promotional emails if they spent over $500"

**Planning Flow**:
1. Router identifies this as a complex task requiring planning
2. TwoStageTaskPlanner is invoked
3. Primary planning stage breaks down into:
   - Query customer orders from database
   - Calculate total spending per customer
   - Filter customers by spending threshold
4. Secondary planning stage details:
   - Specific SQL query generation
   - Email template selection
   - Recipient list preparation
5. Execution phase runs each step with appropriate agents
6. Post-execution verifies completion and stops the loop

## Related Documentation

- [Routing](./routing.md) - Understanding the routing system
- [Agent Utility](./agent-utility.md) - Utility functions for agents
- [Comprehensive ITaskPlanner Guide](../ITASKPLANNER_GUIDE.md) - Detailed technical documentation

For more detailed information including code examples and complete workflows, see the [ITaskPlanner Interface Guide](../ITASKPLANNER_GUIDE.md).
