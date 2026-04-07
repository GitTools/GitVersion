# Prepare Release Milestone

Prepare release tracking on GitHub without creating a release.

## Requirements

1. Describe the release preparation workflow before taking action:
   - Confirm the destination milestone exists or create it.
   - Move the closed issues and closed pull requests from the source milestone to the destination milestone.
   - Verify that every moved issue and pull request has at least one allowed label.
   - Do not create a GitHub release.
2. Ask the user for both:
   - Source milestone
   - Destination milestone
3. Use only these allowed labels when validating moved items:
   - breaking change
   - bug
   - dependencies
   - documentation
   - feature
   - improvement
   - build
4. Treat an item as invalid if:
   - It has no labels.
   - None of its labels are in the allowed list.
5. Report any invalid items clearly and stop before any release creation step.
6. Keep the response concise and operational.
7. Prefer plain non-interactive `gh` commands.
   - Do not prefix commands with `GH_PAGER=cat` by default.
   - Only disable the pager when command output would otherwise not be captured reliably.

## Operational Steps

Use standard `gh` commands by default.
Only disable the pager when needed to make command output reliably readable in the execution environment.

1. Ask:
   - Source milestone?
   - Destination milestone?

2. Summarize the plan:
   - Check whether the destination milestone already exists.
   - Create the destination milestone if needed.
   - Find closed issues in the source milestone.
   - Find closed pull requests in the source milestone.
   - Move those closed items to the destination milestone.
   - Verify labels on all moved items using the allowed label list.
   - Do not create a release.

3. Resolve milestone state:

```bash
gh api repos/{owner}/{repo}/milestones --paginate --jq '.[] | [.number,.title,.state] | @tsv'
gh api repos/{owner}/{repo}/milestones -X POST -f title='<DESTINATION_MILESTONE>'
```

1. Collect closed items from the source milestone:

```bash
gh issue list --repo {owner}/{repo} --milestone '<SOURCE_MILESTONE>' --state closed --limit 200 --json number,title,labels
gh pr list --repo {owner}/{repo} --state closed --search 'milestone:"<SOURCE_MILESTONE>"' --limit 200 --json number,title,labels,state
```

1. Move closed issues and closed pull requests to the destination milestone:

```bash
gh issue edit <ISSUE_NUMBER> --repo {owner}/{repo} --milestone '<DESTINATION_MILESTONE>'
gh pr edit <PR_NUMBER> --repo {owner}/{repo} --milestone '<DESTINATION_MILESTONE>'
```

1. Verify labels against this allowlist:

```text
breaking change
bug
dependencies
documentation
feature
improvement
build
```

Validation rules:

- Each moved item must have at least one label.
- At least one assigned label must match the allowlist exactly.
- If any moved item fails validation, report the item number, title, and labels.

1. Verify final milestone state and report:

```bash
gh issue list --repo {owner}/{repo} --milestone '<DESTINATION_MILESTONE>' --state closed --limit 200 --json number,title,labels
gh pr list --repo {owner}/{repo} --state closed --search 'milestone:"<DESTINATION_MILESTONE>"' --limit 200 --json number,title,labels,state
```

Report:

- Source milestone
- Destination milestone
- Whether the destination milestone was created or already existed
- Count of moved closed issues
- Count of moved closed pull requests
- Any moved items missing allowed labels
- Confirmation that no GitHub release was created
