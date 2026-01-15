---
name: skill-creator
description: A meta-skill that helps create new Agent Skills by providing templates, best practices, and validation guidance.
license: MIT
compatibility: any
metadata:
  author: Maf.AgentSkills
  version: 1.0.0
  category: meta
---

# Skill Creator

This meta-skill helps you create new Agent Skills following the [Agent Skills specification](https://agentskills.io).

## When to Use

Use this skill when:
- Creating a new skill from scratch
- Converting existing documentation into a skill
- Validating an existing skill's structure
- Learning how to write effective skills

## Skill Structure

Every skill must have:

```
skill-name/
├── SKILL.md          # Required: skill definition with YAML frontmatter
├── templates/        # Optional: reusable templates
├── scripts/          # Optional: automation scripts
├── examples/         # Optional: usage examples
└── resources/        # Optional: additional resources
```

## SKILL.md Template

```markdown
---
name: my-skill-name
description: A brief description of what this skill does (max 1024 chars)
license: MIT
compatibility: any
allowed-tools: tool1 tool2 pattern_*
metadata:
  author: Your Name
  version: 1.0.0
  category: category-name
---

# Skill Title

Brief introduction to the skill.

## When to Use

Describe scenarios when this skill should be applied.

## Instructions

Step-by-step instructions for applying the skill.

### Step 1: [Name]
Details...

### Step 2: [Name]
Details...

## Output Format

Describe expected output format if applicable.

## Tips & Best Practices

Additional guidance for effective use.
```

## Naming Rules

Skill names must:
- Use only lowercase letters, numbers, and hyphens
- Start and end with a letter or number
- Be 1-64 characters long
- Match the directory name exactly

**Valid**: `web-research`, `code-review`, `api-client-v2`
**Invalid**: `Web_Research`, `-invalid`, `skill.name`

## Writing Effective Instructions

1. **Be Specific**: Provide clear, actionable steps
2. **Use Examples**: Show input/output examples
3. **Structure Consistently**: Use headings and lists
4. **Include Templates**: Provide reusable templates
5. **Explain Why**: Help users understand the reasoning
6. **Handle Edge Cases**: Document exceptions and limitations

## Best Practices

- Keep skills focused on a single domain/task
- Make skills reusable across different contexts
- Include all necessary context in the skill
- Test skills with various scenarios
- Update skills based on user feedback
- Version skills when making breaking changes
