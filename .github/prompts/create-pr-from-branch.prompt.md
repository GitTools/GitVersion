# Create PR From Branch Changes

Create or update a GitHub pull request from the current branch into the fork's main branch with a well-formatted PR description.

## Requirements

1. Compare changes from origin/main..HEAD and infer the user-facing intent.
2. Keep the PR description aligned with the actual branch changes.
3. The PR description must be well-formatted Markdown.
4. The PR must reference the issue it resolves using closing keywords (for example: Resolves #1234).
5. If no issue number is provided, ask for it before creating or updating the PR.
6. Keep language concise, outcome-focused, and reviewer-friendly.
7. Do not include irrelevant implementation noise; summarize what matters for review.
8. Whenever new commits are pushed that change scope, update the PR description so it stays in sync with current branch changes.

## PR Markdown Template

Use this structure (omit empty sections):
- ## Summary
- ## Why
- ## Validation
- ## Issue

Example:

```md
## Summary
- <high-level change 1>
- <high-level change 2>

## Why
- <problem or goal>

## Validation
- <tests run>
- <results>

## Issue
Resolves #<issue-number>
```

## Operational Steps

1. Ensure branch comparison is current:

```bash
git fetch origin main --quiet
git log --oneline --no-merges origin/main..HEAD
git diff --name-status origin/main..HEAD
```

2. Determine branch and existing PR state:

```bash
BRANCH="$(git rev-parse --abbrev-ref HEAD)"
GH_PAGER=cat GH_FORCE_TTY=0 gh pr list --head "$BRANCH" --json number,title,url,state
```

3. Ask for required metadata if missing:
- Target issue number to resolve
- PR title override (optional)

4. Build or refresh PR body in a temp file:

```bash
cat > /tmp/pr-body.md <<'EOF'
## Summary
- <high-level change 1>
- <high-level change 2>

## Why
- <problem or goal>

## Validation
- <tests run>
- <results>

## Issue
Resolves #<issue-number>
EOF
```

5. Create PR if none exists:

```bash
GH_PAGER=cat GH_FORCE_TTY=0 gh pr create \
  --base main \
  --head "$BRANCH" \
  --title "<PR title>" \
  --body-file /tmp/pr-body.md
```

6. Update PR if one already exists, or after new commits change scope:

```bash
GH_PAGER=cat GH_FORCE_TTY=0 gh pr edit <PR_NUMBER> \
  --title "<PR title>" \
  --body-file /tmp/pr-body.md
```

7. Verify final PR state:

```bash
GH_PAGER=cat GH_FORCE_TTY=0 gh pr view <PR_NUMBER> --json number,title,url,body
```

## Completion Checklist

- PR exists and targets main
- Description is well-formatted Markdown
- Description matches current branch changes
- Issue reference uses closing keyword (Resolves/Fixes/Closes #...)
- PR body was refreshed after any scope-changing push
