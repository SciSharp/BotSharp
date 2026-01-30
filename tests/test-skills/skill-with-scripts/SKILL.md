---
name: skill-with-scripts
description: A test skill demonstrating script bundling with Python and Bash scripts. Use when testing script execution, script discovery, or validating that agents can access and use bundled executable code.
version: 1.0.0
---

# Skill with Scripts

## Overview

This skill demonstrates how to bundle executable scripts with an Agent Skill. It includes both Python and Bash scripts that can be executed by agents.

## Bundled Scripts

### Python Scripts

- **scripts/data_processor.py**: Processes data and returns JSON output
- **scripts/file_analyzer.py**: Analyzes files and generates reports

### Bash Scripts

- **scripts/system_info.sh**: Collects system information
- **scripts/file_operations.sh**: Performs file operations

## Usage Pattern

1. **Discovery**: Agent lists available scripts using `list_skill_directory`
2. **Inspection**: Agent reads script content using `read_skill_file`
3. **Execution**: Agent executes scripts (if filesystem-based integration)
4. **Result Processing**: Agent processes script output

## Script Guidelines

All scripts follow these conventions:
- Support `--help` flag for usage information
- Support `--version` flag for version information
- Return structured output (JSON when possible)
- Exit with appropriate status codes
- Include error handling

## Examples

### Example 1: List Available Scripts
```
Tool: list_skill_directory(skill_name="skill-with-scripts", directory_path="scripts")
Result: [data_processor.py, file_analyzer.py, system_info.sh, file_operations.sh]
```

### Example 2: Read Script Content
```
Tool: read_skill_file(skill_name="skill-with-scripts", file_path="scripts/data_processor.py")
Result: [Python script content]
```

### Example 3: Execute Script (Filesystem-based agents)
```
Command: python /path/to/skills/skill-with-scripts/scripts/data_processor.py --help
Result: [Usage information]
```
