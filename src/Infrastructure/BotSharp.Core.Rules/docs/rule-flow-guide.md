# Rule Flow Graph — Build & Modification Guide

## 1. Graph Structure

A rule flow graph consists of **Nodes** (units of work) connected by **Edges** (directed links). The engine loads the graph, applies schemas from node config, validates connection compatibility, and then traverses the graph starting from the root node.

Every graph must have **exactly one root node** and **exactly one end node**. The root node is the entry point where execution begins, and the end node is the exit point where execution concludes.

## 2. Node Types

Every node **must** have a `Type` and a `Name`. The `Name` must match a registered flow unit implementation.

- **Root** (type: `root` or `start`) — The entry point of the graph. It should have **no** input schema but **must** have an output schema that declares the parameters it passes to downstream nodes. When executed, it copies all trigger parameters into its output.
- **End** (type: `end`) — The exit point of the graph. It can have both an input schema and an output schema. When executed, it collects the final context parameters as its result.
- **Action** (type: `action`) — A node that performs work such as sending a chat message, making an HTTP request, or invoking a tool call. Must have both an input schema and an output schema.
- **Condition** (type: `condition`) — A node that evaluates a boolean expression. Its children are only traversed when the evaluation result is `true`; otherwise the branch is skipped. Must have both an input schema and an output schema.

## 3. Required Node Properties

| Property | Required | Description |
|----------|----------|-------------|
| `Id` | Yes | Unique identifier (GUID). |
| `Name` | Yes | Must match a registered flow unit's name (e.g., `"start"`, `"send_message_to_agent"`). |
| `Type` | Yes | One of: `root`, `start`, `action`, `condition`, `end`. |
| `Config` | No | Key-value dictionary passed as parameters to the node at execution time. Also supports reserved keys (see below). |

## 4. Reserved Config Keys

| Key | Purpose |
|-----|---------|
| `input_schema` | A JSON string describing the node's input schema. When provided, this overrides any default input schema. |
| `output_schema` | A JSON string describing the node's output schema. When provided, this overrides any default output schema. |
| `traversal_algorithm` | The graph traversal defaults to depth-first (DFS). Only set this value if you need to switch the traversal strategy mid-graph. Setting it to `"bfs"` on a node will change the traversal to breadth-first from that node onward. |

## 5. Edge Properties

| Property | Required | Description |
|----------|----------|-------------|
| `Id` | Yes | Unique identifier (GUID). |
| `From` | Yes | Reference to the source node. |
| `To` | Yes | Reference to the target node. |
| `Type` | No | Default `"next"`. |
| `Weight` | No | Higher weight = higher priority when multiple children exist. Default `1.0`. |

## 6. Schema Contract

Each node can declare an **input** and **output** schema to describe its data contract. The schema follows a JSON Schema-like structure with two fields:

- **properties** — A dictionary of parameter names. Each entry has a `type` (one of `string`, `number`, `boolean`, `object`, `array`) and an optional `description`.
- **required** — A list of parameter names that must be provided for the node to function correctly.

Example schema (as JSON):

```json
{
  "properties": {
    "order_id":  { "type": "string",  "description": "The order identifier" },
    "amount":    { "type": "number",  "description": "Total amount" }
  },
  "required": ["order_id"]
}
```

**Schema precedence:** A schema defined in the node's config (via `input_schema` / `output_schema` keys) always **overrides** any default schema.

## 7. Connection Validation Rules

The engine validates **every edge** in the graph at load time. For an edge `[A] → [B]` to be valid:

### Rule 1 — Required keys must be available
**Every key listed in the downstream node's required input must exist in the upstream node's output schema properties.**
**Neither the upstream node's output schema nor the downstream node's input schema can be empty — both must be explicitly defined for every connection**.

### Rule 2 — Types must be compatible
When a required key appears in both the upstream node's output properties and the downstream node's input properties, the type must match (case-insensitive). For example:
- ✅ `A` outputs `{"order_id": {"type": "string"}}` → `B` expects `{"order_id": {"type": "string"}}`
- ❌ `A` outputs `{"amount": {"type": "number"}}` → `B` expects `{"amount": {"type": "string"}}`

### Rule 3 — Boundary constraints
- A graph must contain **exactly one** root node and **exactly one** end node.
- **Root nodes** (`root`/`start`): Should have **no** input schema — they are entry points with no upstream. Must have an output schema that declares the parameters available to downstream nodes.
- **Intermediate nodes** (`action`/`condition`): Must have both an input schema and an output schema. The input schema declares what the node expects from upstream, and the output schema declares what it produces for downstream.
- **End nodes** (`end`): Can have both input and output schemas. The input schema validates that upstream nodes provide the required final data. The output schema describes the data the graph produces as its overall result.

## 8. Built-in Node Reference

### 8.1 `http_request` (Action)

An action node that sends an HTTP request and returns the response. The URL supports placeholder substitution — any `{key}` in the URL is replaced with the matching parameter value at runtime.

**Required config / input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `http_url` | string | Yes | The target URL. May contain `{placeholder}` tokens that are substituted from context parameters. |
| `http_method` | string | Yes | One of: `GET`, `POST`, `PUT`, `DELETE`, `PATCH`. |
| `http_query_params` | array | No | Query parameters as key-value pairs, appended to the URL. |
| `http_request_headers` | array | No | Request headers as key-value pairs. |
| `http_request_body` | string | No | The request body (JSON string). |

**Output:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `http_response` | string | The HTTP response body. |
| `http_response_headers` | string | The HTTP response headers as a JSON string. |

If the request fails (non-success status code or exception), the node returns `Success = false` with an error message.

### 8.2 `logic_gate` (Condition)

A condition node that collects results from multiple parent condition nodes and evaluates a composite logical expression. It waits until **all** parent nodes have been visited before evaluating.

**Required config / input:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `expression` | object | Yes | A JSON-encoded expression tree (see below). |
| `default_value` | string | No | The fallback boolean value (`"true"` or `"false"`) when a referenced parent node or data key is not found. Defaults to `"false"`. |

**Expression tree format:**

There are two kinds of nodes in an expression tree:

- **Leaf node** — References a specific parent node's result.
  - `node` (required): The name of a parent condition node.
  - `key` (optional): A key in the parent node's output data that holds a boolean string. If omitted, the parent's overall success flag is used.

- **Operator node** — Combines child expressions with a logical operator.
  - `op` (required): One of `"and"`, `"or"`, `"not"`.
  - `children` (required): A list of child expression nodes.
  - For `"not"`, only the first child is evaluated.

**Example expression:**

Given three parent condition nodes — `check_work_order`, `check_client`, and `check_affiliate` — the following expression evaluates `work_order_valid AND (client_name_valid OR NOT affiliate_name_valid)`:

```json
{
  "expression": {
    "op": "and",
    "children": [
      { "node": "check_work_order", "key": "work_order_valid" },
      {
        "op": "or",
        "children": [
          { "node": "check_client", "key": "client_name_valid" },
          {
            "op": "not",
            "children": [
              { "node": "check_affiliate", "key": "affiliate_name_valid" }
            ]
          }
        ]
      }
    ]
  },
  "default_value": "false"
}
```

The node produces no output schema — it only controls whether downstream nodes are traversed based on the evaluation result.

## 9. Example: Minimal Valid Graph

```
[start] ──→ [check_order] ──→ [send_message_to_agent] ──→ [end]
  root        condition              action                 end
```

**Node definitions:**

| Node | Type | Name | Output Schema | Input Schema |
|------|------|------|--------------|--------------|
| Start | `root` | `start` | _(from context: `order_id`, `customer_name`)_ | _(none)_ |
| Check Order | `condition` | `check_order` | `{ order_id: string }` | `{ required: [order_id] }` |
| Send Message | `action` | `send_message_to_agent` | `{ agent_id: string, conversation_id: string }` | _(empty — no required)_ |
| End | `end` | `end` | _(none)_ | _(none)_ |

**Validation at load:**
- `[start] → [check_order]`: `check_order` requires `order_id` — satisfied if `start`'s output or `check_order`'s config provides it. ✅
- `[check_order] → [send_message_to_agent]`: `send_message_to_agent` has no required inputs. ✅
- `[send_message_to_agent] → [end]`: `end` has no input schema. ✅

## 10. How Data Flows at Runtime

1. **Start node** — Copies all context parameters (trigger states, flow options) into its output data.
2. **Subsequent nodes** — Receive merged parameters built from three sources in order: the node's own config, then trigger states, then upstream output data.
3. **Condition nodes** — If the evaluation result is `false`, all children of that node are **skipped**.
4. **Action nodes** — If the action result is marked as delayed, children are **deferred** until the next execution cycle.
5. **End node** — Copies the final context parameters into result data.