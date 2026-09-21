---
name: context-search
description: "Semantic code search for discovering and understanding code by meaning rather than keywords.\n- Use this skill when you need to find code whose location you don't already know - e.g., when the task asks 'where is X', 'how does Y work', or describes behavior or intent without naming exact symbols.\n- When not to use: if you already know the relevant file, class, or symbol (use direct navigation or keyword search instead), or for non-code-discovery tasks such as git operations, builds, tests, or reviewing an existing diff."
argument-hint: query

---

# Semantic Code Search

Use `jbcontext search` to find code snippets by meaning, not just keywords.

Use it as a single semantic bootstrap when the relevant file or subsystem is unknown. Do one broad search, open and inspect at least one returned file locally, and inspect nearby code in that same directory or subsystem before any retry. If that still does not identify the needed adjacent area, do a narrowed retry with `jbcontext search -p <path> ...` using the directory of the best first hit.

## Usage

```bash
jbcontext search "<detailed and descriptive query>"
jbcontext search "<code snippet>" # find similar snippets
jbcontext search -p <path> "<query>"  # <path> must be relative to the project root
```

## Query Tips

- Be descriptive: "function that validates user email addresses" > "email"
- Include context: "error handling middleware for HTTP requests with logging"
- Specify what you're looking for: "React component that renders a modal dialog"
- Make the first query specific to the issue's named feature, class, method, or behavior when available

## Examples

```bash
# Find authentication-related code
jbcontext search "user authentication login flow"

# Narrow to specific directory
jbcontext search -p src/auth "JWT token validation"
```