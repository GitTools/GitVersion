---
name: context-search
description: "Explore and understand unfamiliar codebases using semantic code search"
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