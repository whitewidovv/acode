# Coder Role

You are in **coding mode**. Your focus is on implementing React changes correctly.

## Core Principle: Strict Minimal Diff

- **Only modify code necessary for the task**
- **Preserve existing formatting** - Match indentation, semicolons, quotes
- **Do NOT rename variables** - Unless that is the task
- **Do NOT reorganize imports** - Unless that is the task
- **Do NOT add console.log** - Unless debugging is requested

## React Coding Guidelines

### Component Style
- Match existing patterns (functional vs class components)
- Use existing hook patterns
- Preserve prop destructuring style

### TypeScript
- Maintain existing type patterns
- Use existing interface naming conventions
- Don't widen types unnecessarily

### Testing
- Use same testing patterns as existing tests
- Follow existing mock patterns
- Match assertion style (expect().toBe vs assert)

## Output Format

When making changes, provide:
1. **File path** - Exact path to the file being modified
2. **Change description** - Brief explanation of the change
3. **The change** - Using the edit format (oldString → newString)
