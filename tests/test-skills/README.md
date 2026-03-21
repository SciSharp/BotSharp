# Test Skills Directory

This directory contains test skills for validating the Agent Skills plugin implementation.

## Test Skills

### 1. valid-skill
**Purpose**: Comprehensive test skill demonstrating all Agent Skills specification features

**Structure**:
```
valid-skill/
├── SKILL.md              # Complete skill with all optional frontmatter fields
├── scripts/
│   ├── test_script.py    # Python script example
│   └── test_script.sh    # Bash script example
├── references/
│   ├── api_reference.md  # Sample API documentation
│   └── workflow.md       # Sample workflow documentation
└── assets/
    ├── template.txt      # Sample template file
    └── config.json       # Sample configuration file
```

**Tests**:
- Frontmatter parsing (required and optional fields)
- Markdown body parsing
- Script discovery and access
- Reference file access
- Asset file access
- Tool generation (read_skill, read_skill_file, list_skill_directory)

### 2. minimal-skill
**Purpose**: Minimal test skill with only required elements

**Structure**:
```
minimal-skill/
└── SKILL.md              # Minimal skill with only name and description
```

**Tests**:
- Minimal skill loading
- Required fields only (name, description)
- No optional directories
- Basic tool generation

### 3. skill-with-scripts
**Purpose**: Test skill demonstrating script bundling

**Structure**:
```
skill-with-scripts/
├── SKILL.md
└── scripts/
    ├── data_processor.py      # Python script with argparse
    ├── file_analyzer.py       # Python file analysis script
    ├── system_info.sh         # Bash system info script
    └── file_operations.sh     # Bash file operations script
```

**Tests**:
- Script discovery
- Script content reading
- Script execution (filesystem-based agents)
- Multiple script types (Python, Bash)
- Script help and version flags

### 4. large-content-skill
**Purpose**: Test skill with large SKILL.md file (> 50KB)

**Structure**:
```
large-content-skill/
└── SKILL.md              # Large file exceeding MaxOutputSizeBytes
```

**Tests**:
- File size validation
- MaxOutputSizeBytes enforcement
- Error handling for oversized files
- Clear error messages with size information

**File Size**: ~50KB (exceeds typical 50KB limit)

## Usage in Tests

### Unit Tests
```csharp
// Example: Test skill loading
var skillsDir = Path.Combine("tests", "test-skills");
var skills = factory.GetAgentSkills(skillsDir);
Assert.Equal(4, skills.Count);
```

### Integration Tests
```csharp
// Example: Test tool generation
var tools = skillService.GetTools();
Assert.Contains(tools, t => t.Name == "read_skill");
```

### Manual Testing
1. Configure skills directory in appsettings.json:
```json
{
  "AgentSkills": {
    "ProjectSkillsDirectory": "tests/test-skills",
    "EnableProjectSkills": true
  }
}
```

2. Start BotSharp application
3. Verify skills are loaded in logs
4. Test tools in Agent conversations

## Validation

To validate all test skills:

```bash
# Using skills-ref CLI (if available)
skills-ref validate tests/test-skills/valid-skill
skills-ref validate tests/test-skills/minimal-skill
skills-ref validate tests/test-skills/skill-with-scripts
skills-ref validate tests/test-skills/large-content-skill
```

## Expected Behavior

### valid-skill
- ✅ Should load successfully
- ✅ All frontmatter fields should be parsed
- ✅ All directories should be accessible
- ✅ All tools should work

### minimal-skill
- ✅ Should load successfully
- ✅ Only required fields present
- ✅ No directories (should not cause errors)
- ✅ Basic tools should work

### skill-with-scripts
- ✅ Should load successfully
- ✅ Scripts should be discoverable
- ✅ Script content should be readable
- ✅ Scripts should be executable (filesystem-based)

### large-content-skill
- ❌ Should fail to read SKILL.md (exceeds size limit)
- ✅ Should return clear error message
- ✅ Error should include file size and limit
- ✅ Should not crash the application

## Notes

- These skills are for testing purposes only
- Do not use in production environments
- Skills follow the Agent Skills specification from agentskills.io
- All scripts include --help and --version flags
- All scripts return structured output (JSON when possible)
