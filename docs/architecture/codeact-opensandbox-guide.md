# Engineering Guide: CodeAct Agent Orchestration with BotSharp and OpenSandbox

This guide explains a production-oriented orchestration pattern for CodeAct agents in BotSharp, based on the current implementation in the CodeAct plugin.

It focuses on architecture boundaries, runtime lifecycle, safety controls, observability, and rollout strategy.

## 1. Why This Pattern

CodeAct turns LLM-planned code into executable actions. In production, this is high risk unless runtime behavior is constrained and observable.

The BotSharp + OpenSandbox pattern solves this by combining:

- explicit function entry (`execute_code`)
- runtime abstraction (`ICodeActRuntime`)
- sandboxed execution (OpenSandbox)
- default-deny execution posture (read-only pilot)
- structured traces and bounded outputs

## 2. Implementation Topology

The current implementation is split into clear layers:

- Function entry
- Runtime selection and dependency wiring
- Runtime execution policy and lifecycle
- OpenSandbox SDK adapter
- Security bridge and token services

Key files:

- `src/Plugins/BotSharp.Plugin.CodeAct/Functions/ExecuteCodeFn.cs`
- `src/Plugins/BotSharp.Plugin.CodeAct/Runtime/ICodeActRuntime.cs`
- `src/Plugins/BotSharp.Plugin.CodeAct/OpenSandbox/OpenSandboxCodeActRuntime.cs`
- `src/Plugins/BotSharp.Plugin.CodeAct/OpenSandbox/OpenSandboxHttpCodeClient.cs`
- `src/Plugins/BotSharp.Plugin.CodeAct/CodeActPlugin.cs`
- `src/Plugins/BotSharp.Plugin.CodeAct/Settings/CodeActSettings.cs`

## 3. Core Orchestration Flow

The execution path is intentionally narrow and deterministic:

1. The agent invokes `execute_code`.
2. `ExecuteCodeFn` validates and normalizes input.
3. A `CodeActRequest` is created and forwarded to `ICodeActRuntime`.
4. DI chooses the runtime (`fake` or `opensandbox`) via `CodeActSettings.Runtime`.
5. `OpenSandboxCodeActRuntime` enforces policy, creates/reuses session, and streams events.
6. Output is bounded, traced, and converted to `CodeActResult`.
7. `message.Content`, `message.Data`, and `message.StopCompletion` are updated for downstream handling.

## 4. Runtime-Neutral Contract

`ICodeActRuntime` is the key seam that keeps orchestration stable while runtime providers can evolve independently.

Benefits:

- easier A/B runtime testing (`fake` vs `opensandbox`)
- cleaner migration path for future runtimes
- deterministic contract for callers (`ExecuteCodeFn` and bridge layer)

Design rule: all runtime-specific concerns stay below `ICodeActRuntime`.

## 5. Safety Model (Read-Only by Default)

The runtime explicitly rejects non-read-only execution in pilot mode.

Practical controls in current code:

- `ReadOnlyPilot` and request-level `ReadOnly` handling
- explicit empty-code rejection
- bounded stdout/stderr buffers to avoid memory blowups
- operation-level timeout via linked cancellation tokens
- execution traces for both success and failure paths

This is a robust baseline for enterprise rollout.

## 6. OpenSandbox Session Lifecycle Pattern

The runtime supports two lifecycle modes:

- sandbox-per-execution (`CreateSandboxPerExecution = true`)
- shared pre-provisioned sandbox (`SandboxId` configured)

When the runtime owns the session, it creates and destroys sandbox resources in the same execution scope.

The SDK adapter layer (`OpenSandboxHttpCodeClient`) handles:

- sandbox create/connect
- interpreter create
- context create/reuse
- streaming event mapping
- sandbox kill/dispose

This separation keeps orchestration readable and side effects localized.

## 7. Event Streaming and Result Shaping

OpenSandbox stream events are normalized into internal `OpenSandboxCodeEvent` records.

Current mapping includes:

- stdout
- stderr
- error
- completed

Result shaping strategy:

- prefer stdout as primary content when available
- fallback to stderr content
- preserve error code/message for machine handling
- include metadata (`runtime`, `sandbox_id`, `context_id`, `sandbox_created`)

This gives both human-readable and automation-friendly outputs.

## 8. Configuration Surface

`CodeActSettings` centralizes runtime and policy behavior.

Important OpenSandbox settings:

- connectivity and auth: `ControlPlaneBaseUrl`, `ApiKey`
- runtime image and startup: `RuntimeImage`, `Entrypoint`
- language and execution: `Language`, `SandboxTtlSeconds`, `ExecutionTimeoutSeconds`
- resource control: `CpuLimit`, `MemoryMb`
- output/trace limits: `MaxStdoutChars`, `MaxStderrChars`, `MaxTraceEvents`
- lifecycle strategy: `CreateSandboxPerExecution`, `SandboxId`

Minimal example:

```json
{
  "CodeAct": {
    "Enabled": true,
    "ExposeExecuteCode": true,
    "Runtime": "opensandbox",
    "ReadOnlyPilot": true,
    "ExecutionTimeoutSeconds": 10,
    "OpenSandbox": {
      "ControlPlaneBaseUrl": "https://api.opensandbox.io",
      "ApiKey": "${OPEN_SANDBOX_API_KEY}",
      "RuntimeImage": "opensandbox/code-interpreter:v1.0.2",
      "Entrypoint": ["/opt/opensandbox/code-interpreter.sh"],
      "Language": "python",
      "CreateSandboxPerExecution": true,
      "SandboxTtlSeconds": 300,
      "MaxStdoutChars": 20000,
      "MaxStderrChars": 12000,
      "MaxTraceEvents": 200
    }
  }
}
```

## 9. DI and Extension Pattern

`CodeActPlugin` wires all orchestration parts in a composable way:

- function callback registration (`ExecuteCodeFn`)
- runtime registration and selector
- OpenSandbox client adapter
- security policy and token service
- bridge integration

This keeps extension points explicit and testable.

## 10. Reliability and Observability Recommendations

For production readiness, keep the existing model and add the following:

- propagate request IDs into centralized logs
- emit sandbox create/connect/kill metrics
- track timeout and truncation rates
- alert on repeated `opensandbox.execution_error` and `opensandbox.timeout`
- keep `MaxTraceEvents` high enough for incident reconstruction

## 11. Deployment Strategy

Recommended rollout:

1. Start with `ReadOnlyPilot = true` and sandbox-per-execution.
2. Run with strict output and timeout limits.
3. Observe traces and tune limits by workload profile.
4. Introduce shared sandbox mode only when behavior is stable.
5. Expand language coverage incrementally.

## 12. Anti-Patterns to Avoid

- bypassing `ICodeActRuntime` from function layer
- allowing unbounded output accumulation
- mixing sandbox SDK details into orchestration function logic
- disabling trace capture in production
- enabling mutable execution before policy and approval flow are mature

## 13. Test Checklist

Use this checklist when validating changes:

- invalid JSON in function args returns structured failure
- empty code is rejected deterministically
- non-read-only request is rejected in pilot mode
- timeout returns `opensandbox.timeout`
- stream error maps to `opensandbox.execution_error`
- stdout/stderr truncation produces trace events
- owned sandbox is destroyed on completion and failure

## 14. Summary

The BotSharp CodeAct implementation demonstrates a strong enterprise pattern:

- narrow and explicit execution entry
- runtime abstraction for portability
- sandbox-backed execution for isolation
- policy-first controls for safety
- trace-first design for operations

Use this as the default blueprint for code-capable agent features, then scale with stronger approval workflows, richer policy enforcement, and deeper runtime telemetry.
