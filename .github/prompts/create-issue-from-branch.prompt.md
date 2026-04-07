# Create Issue From Branch Changes

Create or update a GitHub issue from the changes in the current branch compared to main.

## Requirements

1. Compare changes from origin/main..HEAD and infer the user-facing goal.
2. Do not include implementation details (no file lists, refactor notes, internal mechanics).
3. Ask before editing the issue:
   - Which label should be used?
   - Which milestone should be used?
4. Defaults:
   - If label is not provided, use improvement.
   - If milestone is not provided, use the latest open milestone.
5. The issue description must be well-formatted Markdown.
6. Use this Markdown structure exactly (omit empty sections):

   - ## Summary

   - ## Goal

   - ## Success Criteria

   - ## References

7. Keep language concise, goal-oriented, and user-facing.
8. Prefer plain non-interactive `gh` commands.
   - Do not prefix commands with `GH_PAGER=cat` by default.
   - Only disable the pager when command output would otherwise not be captured reliably.

## Operational Steps

Use standard `gh` commands by default.
Only disable the pager when needed to make command output reliably readable in the execution environment.

1. Ensure branch comparison is up to date:

```bash
git fetch origin main --quiet
git log --oneline --no-merges origin/main..HEAD
git diff --name-status origin/main..HEAD
```

1. Ask:
   - Label? (default: improvement)
   - Milestone? (default: latest open milestone)

2. Resolve defaults:

```bash
LABEL="${LABEL:-improvement}"

if [ -z "$MILESTONE" ]; then
  MILESTONE="$(gh api repos/{owner}/{repo}/milestones --paginate -f state=open --jq 'sort_by(.number) | last | .title')"
fi
```

1. Build a well-formatted Markdown body in a temp file:

```bash
cat > /tmp/issue-body.md <<'EOF'
## Summary
<goal-focused summary>

## Goal
<what outcome we want>

## Success Criteria
- <criterion 1>
- <criterion 2>

## References
- <optional links>
EOF
```

1. Suggest a title in this style:
   - Adapt prefix to selected label where appropriate (for example: [FEATURE]: ...).

2. Create or update issue:

Create:

```bash
gh issue create \
  --title "<TITLE>" \
  --label "$LABEL" \
  --milestone "$MILESTONE" \
  --body-file /tmp/issue-body.md
```

Update existing:

```bash
gh issue edit <ISSUE_NUMBER> \
  --title "<TITLE>" \
  --add-label "$LABEL" \
  --milestone "$MILESTONE" \
  --body-file /tmp/issue-body.md
```

1. Verify and report final state:

```bash
gh issue view <ISSUE_NUMBER> --json number,title,url,labels,milestone,body
```

Report:

- Issue number and URL
- Final title
- Final label(s)
- Final milestone
- Confirmation that body is well-formatted Markdown and goal-focused
