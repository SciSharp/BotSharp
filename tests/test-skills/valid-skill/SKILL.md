---
name: valid-skill
description: A complete test skill demonstrating all Agent Skills specification features including scripts, references, and assets. Use when testing the full Agent Skills implementation or validating skill loading functionality.
version: 1.0.0
author: BotSharp Test Suite
tags: [test, validation, complete]
---

# Valid Skill

## Overview

This is a comprehensive test skill that demonstrates all features of the Agent Skills specification. It includes:
- Complete YAML frontmatter with all optional fields
- Structured markdown content
- Scripts for executable operations
- Reference documentation
- Asset files

## Workflow

1. **Initialization**: Load the skill metadata from frontmatter
2. **Instruction Reading**: Parse the markdown body for instructions
3. **Resource Access**: Access bundled scripts, references, and assets as needed
4. **Execution**: Execute scripts or use references to complete tasks

## Use Cases

### Use Case 1: Test Skill Loading
When testing if the Agent Skills plugin correctly loads skills:
- Verify frontmatter parsing (name, description, version, author, tags)
- Confirm markdown body is accessible
- Check that all directories are recognized

### Use Case 2: Test Resource Access
When testing resource file access:
- Read scripts from `scripts/` directory
- Access documentation from `references/` directory
- Retrieve assets from `assets/` directory

### Use Case 3: Test Tool Generation
When testing tool generation from skills:
- Verify `read_skill` tool returns this content
- Verify `read_skill_file` can access bundled files
- Verify `list_skill_directory` shows correct structure

## Examples

### Example 1: Reading the Skill
```
Agent: I need to understand the valid-skill capabilities
Tool: read_skill(skill_name="valid-skill")
Result: [This entire SKILL.md content]
```

### Example 2: Accessing a Script
```
Agent: Show me the test script
Tool: read_skill_file(skill_name="valid-skill", file_path="scripts/test_script.py")
Result: [Python script content]
```

### Example 3: Listing Resources
```
Agent: What files are available in this skill?
Tool: list_skill_directory(skill_name="valid-skill", directory_path=".")
Result: [SKILL.md, scripts/, references/, assets/]
```

## Reference Files

- **scripts/test_script.py**: A simple Python script for testing script execution
- **scripts/test_script.sh**: A simple Bash script for testing shell script execution
- **references/api_reference.md**: Sample API documentation
- **references/workflow.md**: Sample workflow documentation
- **assets/template.txt**: Sample template file
- **assets/config.json**: Sample configuration file

## Notes

This skill is designed for testing purposes only. It demonstrates the complete structure and capabilities of the Agent Skills specification but does not perform any real-world operations.

## Validation Checklist

- [x] YAML frontmatter with required fields (name, description)
- [x] YAML frontmatter with optional fields (version, author, tags)
- [x] Structured markdown body with clear sections
- [x] Scripts directory with executable files
- [x] References directory with documentation
- [x] Assets directory with resource files
- [x] Clear use cases and examples
- [x] Proper formatting and organization
