---
name: web-research
description: A skill for conducting comprehensive web research on any topic, synthesizing information from multiple sources into well-organized summaries.
license: MIT
compatibility: any
allowed-tools: read_file web_search fetch_url
metadata:
  author: Maf.AgentSkills
  version: 1.0.0
  category: research
---

# Web Research Skill

This skill helps you conduct comprehensive web research on any topic.

## When to Use

Use this skill when the user asks you to:
- Research a topic thoroughly
- Find and summarize information from multiple sources
- Create a research report or literature review
- Compare different perspectives on a subject

## Instructions

### 1. Clarify the Research Scope

Before starting, ensure you understand:
- The specific topic or question
- The depth of research required (quick overview vs. deep dive)
- Any specific sources or domains to focus on
- The desired output format

### 2. Search Strategy

1. **Start broad**: Use general search queries to understand the landscape
2. **Narrow down**: Use specific keywords based on initial findings
3. **Diversify sources**: Look for academic, industry, and news perspectives
4. **Verify facts**: Cross-reference important claims across sources

### 3. Information Synthesis

When synthesizing information:
- Organize by themes, not by source
- Highlight areas of consensus and disagreement
- Note the credibility and recency of sources
- Identify gaps in available information

### 4. Output Format

Structure your research report as:

```markdown
# Research Report: [Topic]

## Executive Summary
[2-3 sentence overview]

## Key Findings
1. [Finding 1]
2. [Finding 2]
...

## Detailed Analysis

### [Theme 1]
[Analysis with citations]

### [Theme 2]
[Analysis with citations]

## Sources
- [Source 1]: [URL]
- [Source 2]: [URL]
...

## Limitations & Further Research
[What wasn't covered, what needs more investigation]
```

## Tips

- Always cite your sources
- Be transparent about the limitations of your research
- Distinguish between facts, expert opinions, and speculation
- Update findings if you discover contradictory information
