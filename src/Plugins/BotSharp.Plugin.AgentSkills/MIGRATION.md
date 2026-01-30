# Migration Guide: Agent Skills Plugin

This guide helps you migrate to the new Agent Skills plugin implementation based on the [Agent Skills specification](https://agentskills.io) and [AgentSkillsDotNet](https://github.com/microsoft/agentskills-dotnet) library.

## Overview

The Agent Skills plugin has been refactored to:
- Follow the official Agent Skills specification
- Use Microsoft's AgentSkillsDotNet library
- Provide better performance and security
- Support progressive disclosure pattern
- Improve maintainability and extensibility

## Breaking Changes

### 1. Configuration Changes

**Old Configuration (if you had custom settings):**
```json
{
  "AgentSkills": {
    "SkillsDirectory": "skills"
  }
}
```

**New Configuration:**
```json
{
  "AgentSkills": {
    "EnableProjectSkills": true,
    "EnableUserSkills": false,
    "ProjectSkillsDirectory": "AgentSkills",
    "UserSkillsDirectory": "~/.agent-skills",
    "MaxOutputSizeBytes": 51200,
    "EnableReadFileTool": true
  }
}
```

**Migration Steps:**
1. Update your `appsettings.json` with the new configuration structure
2. Rename `SkillsDirectory` to `ProjectSkillsDirectory` (if applicable)
3. Set `EnableProjectSkills` to `true`
4. Add other configuration options with default values

### 2. Skill Directory Structure

**Old Structure (if different):**
```
skills/
└── my-skill/
    └── skill.md
```

**New Structure (Agent Skills Specification):**
```
AgentSkills/
└── my-skill/
    ├── SKILL.md              # Required (uppercase)
    ├── scripts/              # Optional
    │   └── process.py
    ├── references/           # Optional
    │   └── guide.md
    └── assets/               # Optional
        └── config.json
```

**Migration Steps:**
1. Rename `skill.md` to `SKILL.md` (uppercase)
2. Add frontmatter to SKILL.md files (see below)
3. Organize scripts, references, and assets into subdirectories
4. Move skills to the configured `ProjectSkillsDirectory`

### 3. SKILL.md Format

**Old Format (if you had custom format):**
```markdown
# My Skill

Description of the skill...
```

**New Format (Agent Skills Specification):**
```markdown
---
name: my-skill
description: Brief description of what this skill does
---

# My Skill

## Instructions

Detailed instructions for the AI agent...

## Examples

Usage examples...
```

**Migration Steps:**
1. Add YAML frontmatter with `name` and `description`
2. Ensure `name` matches the directory name
3. Add `## Instructions` section for agent guidance
4. Add `## Examples` section (optional but recommended)

### 4. Tool Names

**Old Tool Names (if different):**
- `list_skills`
- `get_skill`
- `read_file`

**New Tool Names:**
- `get-available-skills` (replaces `list_skills`)
- `read_skill` (replaces `get_skill`)
- `read_skill_file` (replaces `read_file`)

**Migration Steps:**
1. Update any agent instructions that reference old tool names
2. Update any custom code that calls these tools
3. Test agent interactions with new tool names

### 5. API Changes

If you were using the plugin programmatically:

**Old API (if you had custom integration):**
```csharp
// Old way (example)
var skills = skillManager.GetAllSkills();
```

**New API:**
```csharp
// New way
var skillService = serviceProvider.GetRequiredService<ISkillService>();
var skills = skillService.GetAgentSkills();
var instructions = skillService.GetInstructions();
var tools = skillService.GetTools();
```

**Migration Steps:**
1. Replace old service references with `ISkillService`
2. Update method calls to use new API
3. Handle `AgentSkills` type from AgentSkillsDotNet library

## Migration Steps

### Step 1: Update Configuration

1. Open your `appsettings.json` file
2. Update the `AgentSkills` section with new configuration:

```json
{
  "AgentSkills": {
    "EnableProjectSkills": true,
    "ProjectSkillsDirectory": "AgentSkills",
    "MaxOutputSizeBytes": 51200,
    "EnableReadFileTool": true
  }
}
```

3. Save the file

### Step 2: Update Skill Files

For each skill in your skills directory:

1. **Rename skill.md to SKILL.md** (if needed):
   ```bash
   mv skills/my-skill/skill.md skills/my-skill/SKILL.md
   ```

2. **Add frontmatter** to SKILL.md:
   ```markdown
   ---
   name: my-skill
   description: Brief description
   ---
   
   [Rest of your content]
   ```

3. **Organize files** into subdirectories:
   ```bash
   mkdir -p skills/my-skill/scripts
   mkdir -p skills/my-skill/references
   mkdir -p skills/my-skill/assets
   
   # Move files to appropriate directories
   mv skills/my-skill/*.py skills/my-skill/scripts/
   mv skills/my-skill/*.md skills/my-skill/references/ # except SKILL.md
   mv skills/my-skill/*.json skills/my-skill/assets/
   ```

### Step 3: Move Skills Directory

If your skills are not in the default location:

1. Move skills to the configured directory:
   ```bash
   mv skills AgentSkills
   ```

2. Or update configuration to point to your existing directory:
   ```json
   {
     "AgentSkills": {
       "ProjectSkillsDirectory": "path/to/your/skills"
     }
   }
   ```

### Step 4: Update Agent Instructions

If you have custom agent instructions that reference skills:

1. Update tool names:
   - `list_skills` → `get-available-skills`
   - `get_skill` → `read_skill`
   - `read_file` → `read_skill_file`

2. Update any skill-specific references to match new names

### Step 5: Test the Migration

1. **Start the application**:
   ```bash
   dotnet run
   ```

2. **Check logs** for skill loading:
   ```
   info: BotSharp.Plugin.AgentSkills.Services.SkillService[0]
         Initializing Agent Skills...
   info: BotSharp.Plugin.AgentSkills.Services.SkillService[0]
         Loaded 3 project skills
   ```

3. **Test with an agent**:
   - Create a test conversation
   - Ask the agent to list available skills
   - Verify the agent can read skill details

4. **Verify tools are available**:
   - Check that `get-available-skills` returns your skills
   - Test `read_skill` with a skill name
   - Test `read_skill_file` with a file path

### Step 6: Verify Functionality

Run through these verification steps:

- [ ] Application starts without errors
- [ ] Skills are loaded (check logs)
- [ ] Agent can see available skills
- [ ] Agent can read skill details
- [ ] Agent can read skill files
- [ ] No errors in logs related to skills

## Common Migration Issues

### Issue 1: Skills Not Loading

**Symptoms:**
- Log message: "Project skills directory not found"
- Agent doesn't see any skills

**Solution:**
1. Check `ProjectSkillsDirectory` path in configuration
2. Ensure directory exists and contains valid skills
3. Verify SKILL.md files have correct frontmatter

### Issue 2: Invalid SKILL.md Format

**Symptoms:**
- Skills load but content is missing
- Errors parsing skill metadata

**Solution:**
1. Ensure SKILL.md has YAML frontmatter with `---` delimiters
2. Verify `name` and `description` fields are present
3. Check for YAML syntax errors

### Issue 3: Tool Names Not Working

**Symptoms:**
- Agent can't find tools
- "Tool not found" errors

**Solution:**
1. Update tool names in agent instructions
2. Restart application after configuration changes
3. Check that `EnableReadFileTool` is `true` if using `read_skill_file`

### Issue 4: File Size Errors

**Symptoms:**
- "File size exceeds limit" errors
- Can't read certain skill files

**Solution:**
1. Increase `MaxOutputSizeBytes` in configuration
2. Split large files into smaller chunks
3. Store large files outside skill directory

### Issue 5: Path Security Errors

**Symptoms:**
- "Access denied" errors
- "Unauthorized access" warnings

**Solution:**
1. Don't use `..` in file paths
2. Use relative paths within skill directory
3. Verify files exist in skill's directory structure

## Rollback Plan

If you need to rollback the migration:

1. **Restore old configuration**:
   - Revert `appsettings.json` to previous version
   - Restore old skill directory structure

2. **Restore old skill files**:
   - Rename SKILL.md back to skill.md (if needed)
   - Remove frontmatter
   - Move files back to flat structure

3. **Restart application**:
   ```bash
   dotnet run
   ```

## Post-Migration Checklist

After completing the migration:

- [ ] All skills load successfully
- [ ] Configuration is correct and validated
- [ ] SKILL.md files have proper frontmatter
- [ ] Directory structure follows specification
- [ ] Agent can interact with skills
- [ ] All tools work correctly
- [ ] No errors in application logs
- [ ] Performance is acceptable
- [ ] Documentation is updated
- [ ] Team is informed of changes

## Getting Help

If you encounter issues during migration:

1. **Check logs**: Look for error messages in application logs
2. **Verify configuration**: Ensure all settings are correct
3. **Test with examples**: Use provided example skills to verify setup
4. **Review documentation**: Check README.md for detailed information
5. **Report issues**: Create an issue on GitHub if problems persist

## Additional Resources

- [Agent Skills Specification](https://agentskills.io)
- [AgentSkillsDotNet Library](https://github.com/microsoft/agentskills-dotnet)
- [Plugin README](README.md)
- [Example Skills](../../tests/test-skills/)
- [BotSharp Documentation](https://github.com/SciSharp/BotSharp)

## FAQ

### Q: Do I need to migrate immediately?

A: The new implementation provides better performance, security, and standards compliance. We recommend migrating when convenient, but there's no immediate deadline.

### Q: Will my old skills work without changes?

A: Most skills will need minor updates (SKILL.md frontmatter, directory structure). The migration is straightforward and documented above.

### Q: Can I use both old and new formats?

A: No, the plugin now only supports the Agent Skills specification format. All skills must be migrated.

### Q: What if I have many skills to migrate?

A: You can write a script to automate the migration:
1. Rename files
2. Add frontmatter
3. Organize into subdirectories

### Q: How do I validate my migrated skills?

A: Use the test skills in `tests/test-skills/` as reference examples. Ensure your skills follow the same structure.

### Q: What happens to custom skill loaders?

A: Custom loaders are no longer needed. The AgentSkillsDotNet library handles all skill loading and validation.

### Q: Can I customize skill loading behavior?

A: Configuration options allow customization of directories, file size limits, and tool availability. For advanced customization, extend `ISkillService`.

### Q: How do I report migration issues?

A: Create an issue on the BotSharp GitHub repository with:
- Your configuration
- Skill structure
- Error messages
- Steps to reproduce

## Version History

- **v5.3.0** (2026-01): Initial release of refactored plugin
  - Implemented Agent Skills specification
  - Integrated AgentSkillsDotNet library
  - Added progressive disclosure support
  - Improved security and performance

## Support

For additional support:
- GitHub Issues: [BotSharp Issues](https://github.com/SciSharp/BotSharp/issues)
- Documentation: [Plugin README](README.md)
- Specification: [agentskills.io](https://agentskills.io)
