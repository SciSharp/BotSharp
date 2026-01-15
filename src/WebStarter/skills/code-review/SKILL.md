---
name: code-review
description: A skill for conducting thorough code reviews, identifying issues, suggesting improvements, and ensuring code quality standards are met.
license: MIT
compatibility: any
allowed-tools: read_file list_directory grep_search
metadata:
  author: Maf.AgentSkills
  version: 1.0.0
  category: development
---

# Code Review Skill

This skill helps you conduct thorough and constructive code reviews.

## When to Use

Use this skill when:
- Reviewing pull requests or code changes
- Auditing code quality in a project
- Helping developers improve their code
- Checking for security vulnerabilities or bugs

## Review Checklist

Use the checklist in `templates/review-checklist.md` to ensure comprehensive coverage.

### Categories

1. **Correctness**: Does the code do what it's supposed to do?
2. **Security**: Are there any security vulnerabilities?
3. **Performance**: Are there performance concerns?
4. **Maintainability**: Is the code easy to understand and modify?
5. **Testing**: Is the code adequately tested?
6. **Documentation**: Is the code well-documented?

## Instructions

### 1. Understand Context

Before reviewing:
- Understand the purpose of the change
- Read any related issue or ticket
- Know the project's coding standards

### 2. Review Systematically

Go through the code in this order:
1. **Architecture**: Does the overall approach make sense?
2. **Logic**: Is the logic correct and complete?
3. **Edge Cases**: Are edge cases handled?
4. **Error Handling**: Are errors handled appropriately?
5. **Style**: Does the code follow conventions?

### 3. Provide Constructive Feedback

For each issue found:
- Explain **what** the issue is
- Explain **why** it's a problem
- Suggest **how** to fix it
- Categorize severity (blocker, major, minor, suggestion)

### 4. Output Format

```markdown
# Code Review: [File/PR Name]

## Summary
[Overall assessment: approve, request changes, or comment]

## Critical Issues 🔴
[Issues that must be fixed before merge]

## Major Issues 🟠
[Important issues that should be addressed]

## Minor Issues 🟡
[Nice-to-have improvements]

## Suggestions 💡
[Optional improvements for consideration]

## Positive Highlights ✨
[Things done well - always include some!]
```

## Best Practices

- Be respectful and constructive
- Focus on the code, not the person
- Ask questions when unclear
- Acknowledge good patterns
- Suggest alternatives, don't just criticize
