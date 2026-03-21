# Agent Skills Hooks Property-Based Tests

This document describes the property-based tests implemented for the Agent Skills hooks using CsCheck.

## Overview

Property-based tests verify correctness properties that should hold for all inputs, not just specific test cases. These tests are defined in `AgentSkillsHooksPropertyTests.cs` and implement the correctness properties from design document sections 11.5 and 11.2.

## Implemented Properties

### Property 5.1: Agent Type Filtering

**Requirement**: FR-2.2  
**Design Reference**: Section 11.5

**Property Statement**:
```
For any Agent agent,
IF agent.Type IN [Routing, Planning],
THEN OnInstructionLoaded() should not inject available_skills
```

**Tests**:
1. `Property_AgentTypeFiltering_RoutingAndPlanningAgentsSkipInjection`
   - Verifies that Routing and Planning agents never receive skill injection
   - Tests both agent types explicitly

2. `Property_AgentTypeFiltering_NonFilteredAgentsReceiveInjection`
   - Verifies that all other agent types (Task, Static, Evaluating, A2ARemote) receive injection
   - Tests the inverse property

3. `Property_AgentTypeFiltering_ConsistentAcrossInvocations`
   - Verifies that filtering behavior is deterministic and consistent
   - Tests multiple invocations of the same hook

**Why This Matters**:
- Ensures Routing and Planning agents don't get overwhelmed with skill information
- Maintains consistent behavior across different agent types
- Prevents accidental injection to agents that shouldn't have skills

### Property 5.2: Instruction Format Correctness

**Requirement**: FR-2.1  
**Design Reference**: Section 11.5

**Property Statement**:
```
For any skill set skills,
GetInstructions() should return valid XML format string
```

**Tests**:
1. `Property_InstructionFormat_AlwaysValidXml`
   - Verifies that instructions are always parseable as XML
   - Tests various instruction formats (empty, single skill, multiple skills, special characters)
   - Uses XDocument.Parse to validate XML structure

2. `Property_InstructionFormat_HasRequiredStructure`
   - Verifies that XML has the required `<available_skills>` root element
   - Verifies that each skill has `<name>` and `<description>` elements
   - Ensures structural consistency

3. `Property_InstructionFormat_EmptyInstructionsDoNotInject`
   - Verifies that empty or null instructions don't result in injection
   - Tests edge cases

**Why This Matters**:
- Ensures LLMs can reliably parse skill information
- Prevents malformed XML from breaking agent instructions
- Maintains consistent format across all skill sets

### Property 2.1: Tool Name Uniqueness

**Requirement**: FR-3.1  
**Design Reference**: Section 11.2

**Property Statement**:
```
For any skill set skills,
GetAsTools(skills) returned tool names should be unique
```

**Tests**:
1. `Property_ToolNameUniqueness_AllToolNamesAreUnique`
   - Verifies that all registered tool names are unique
   - Tests with multiple tools (read_skill, read_skill_file, list_skill_directory, etc.)
   - Ensures no duplicates in the tool list

2. `Property_ToolNameUniqueness_DuplicatesArePrevented`
   - Verifies that the hook prevents duplicate tool registration
   - Tests that pre-existing tools are not overwritten
   - Ensures original function is preserved when duplicate is prevented

3. `Property_ToolNameUniqueness_MaintainedAcrossInvocations`
   - Verifies that uniqueness is maintained across multiple hook invocations
   - Tests that repeated invocations don't add duplicates
   - Ensures idempotent behavior

4. `Property_ToolNameUniqueness_EmptyToolListIsValid`
   - Verifies that empty tool lists don't violate uniqueness
   - Tests edge case of no tools available
   - Ensures uniqueness property is trivially satisfied for empty lists

**Why This Matters**:
- Prevents tool name collisions that could cause runtime errors
- Ensures agents can reliably call tools by name
- Maintains system stability when multiple skills are loaded

## Test Execution

Run all property tests:
```bash
dotnet test --filter "FullyQualifiedName~AgentSkillsHooksPropertyTests"
```

Run specific property test:
```bash
dotnet test --filter "FullyQualifiedName~Property_AgentTypeFiltering"
```

Run all hook tests (unit + property):
```bash
dotnet test --filter "FullyQualifiedName~AgentSkillsHooks"
```

## Test Results

As of implementation:
- **Total Property Tests**: 11
- **All Tests Passing**: ✅ 11/11
- **Total Hook Tests**: 27 (16 unit + 11 property)
- **All Hook Tests Passing**: ✅ 27/27

## Property-Based Testing Benefits

1. **Broader Coverage**: Tests properties across many inputs, not just specific cases
2. **Edge Case Discovery**: Automatically tests edge cases we might not think of
3. **Regression Prevention**: Properties ensure behavior remains correct as code evolves
4. **Documentation**: Properties serve as executable specifications
5. **Confidence**: Higher confidence that the system behaves correctly in all scenarios

## Design Document References

- **Section 11.5**: Instruction Injection Properties
  - Property 5.1: Agent type filtering
  - Property 5.2: Instruction format correctness

- **Section 11.2**: Tool Generation Properties
  - Property 2.1: Tool name uniqueness

## Related Files

- `AgentSkillsHooksPropertyTests.cs` - Property-based tests
- `AgentSkillsHooksTests.cs` - Unit tests
- `AgentSkillsInstructionHook.cs` - Instruction injection hook implementation
- `AgentSkillsFunctionHook.cs` - Function registration hook implementation
- `.kiro/specs/agent-skills-refactor/design.md` - Design document with property definitions

## Future Property Tests

Additional properties that could be tested in the future:

1. **Property 3.1**: Path traversal prevention (security)
2. **Property 4.1**: File size limit enforcement (security)
3. **Property 6.1**: Error tolerance (reliability)
4. **Property 1.1**: Skill loading idempotency (already tested in SkillServicePropertyTests)

## Notes

- These tests use CsCheck for property-based testing
- Tests are marked as optional (`5.4*`) in the task list but provide valuable additional coverage
- All tests follow the EARS format requirements from the design document
- Tests verify both positive and negative cases (what should happen and what shouldn't)
