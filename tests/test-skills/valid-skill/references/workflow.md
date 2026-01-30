# Workflow Documentation

## Overview

This document describes sample workflows for testing the Agent Skills reference documentation functionality.

## Standard Workflow

### Step 1: Initialization

1. Load the skill metadata
2. Parse the YAML frontmatter
3. Validate required fields (name, description)
4. Cache the skill instance

### Step 2: Instruction Loading

1. Agent identifies relevant skill based on description
2. Agent calls `read_skill` tool with skill name
3. System returns full SKILL.md content
4. Agent parses instructions and plans execution

### Step 3: Resource Access

1. Agent determines which resources are needed
2. Agent calls `read_skill_file` for specific files
3. System validates path security (no path traversal)
4. System returns file content if within size limits

### Step 4: Execution

1. Agent follows instructions from SKILL.md
2. Agent may execute scripts or use reference data
3. Agent produces output based on skill guidance
4. Agent logs operations for audit trail

## Error Handling Workflow

### Path Traversal Attempt

1. Agent requests file with `../` in path
2. System detects path traversal attempt
3. System rejects request with security error
4. System logs security event

### File Size Limit Exceeded

1. Agent requests large file
2. System checks file size against limit
3. System rejects if size > MaxOutputSizeBytes
4. System returns error with size information

### File Not Found

1. Agent requests non-existent file
2. System checks file existence
3. System returns FileNotFoundException
4. Agent handles error gracefully

## Best Practices

1. **Always validate inputs**: Check skill names and file paths
2. **Use progressive disclosure**: Load metadata first, full content on demand
3. **Implement caching**: Cache skill instances to improve performance
4. **Log operations**: Record skill loading and tool calls for debugging
5. **Handle errors gracefully**: Provide clear error messages to agents
