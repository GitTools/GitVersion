# Prepare Release

Prepare a GitHub release after confirming the target version and release type.

## Requirements

1. Describe the release workflow before taking action:
   - Confirm whether the release should be published as a prerelease or a release.
   - After the release type is confirmed, check for an open milestone that is a concrete version and not a spec milestone such as `4.x` or `4.5.x`.
   - If a valid open milestone exists, ask the user to confirm that version with a yes/no question.
   - If no valid open milestone exists, require the user to provide the release version.
   - Verify whether the tag already exists.
   - Verify whether a GitHub release already exists for the version.
   - Create or update the GitHub release with the title set to the version.
2. Ask the user for both:
   - Release type: prerelease or release
   - Release version (or milestone confirmation yes/no when a valid milestone exists)
3. Use the release version as both:
   - Tag name
   - Release title
4. If the user selects prerelease:
   - Create the release with the prerelease flag enabled.
5. If the user selects release:
   - Create the release without the prerelease flag.
   - Ensure the release is linked to the repository announcement discussion for that version.
6. Treat milestone titles such as `v4.x` or `v4.5.x` as spec milestones and do not suggest them as the release version.
7. If a release already exists for the selected tag:
   - Do not fail or stop at `gh release create`.
   - Inspect the existing release and update it instead.
   - If the requested type is release and the existing release is a prerelease, convert it to a full release.
   - If the requested type is release and there is no linked discussion, link or create the announcement discussion during the update.
8. Keep the response concise and operational.
9. Prefer plain non-interactive `gh` commands.
   - Do not prefix commands with `GH_PAGER=cat` by default.
   - Only disable the pager when command output would otherwise not be captured reliably.

## Operational Steps

1. Ask:
   - Is this a prerelease or a release?

Ask for the release type first.

After release type is selected, check open milestones before asking for a version.

Use standard `gh` commands by default.
Only disable the pager when needed to make command output reliably readable in the execution environment.

1. Summarize the plan:
   - Confirm whether this should be a prerelease or a release.
   - Inspect open milestones and find the latest concrete version milestone.
   - If found, ask the user to confirm that version with yes/no.
   - If not found, ask the user to provide a release version.
   - Verify whether the tag already exists.
   - Verify whether a GitHub release already exists for that tag.
   - Create or update the GitHub release using the version as the tag and title.
   - Apply the prerelease flag only when requested.
   - When publishing a full release, ensure the `Announcements` discussion is linked.

2. After the release type is selected, inspect open milestones and find the latest non-spec milestone:

```bash
gh api repos/{owner}/{repo}/milestones --paginate --jq '.[] | select(.state == "open") | .title'
```

Suggestion rules:

- Exclude milestone titles that match spec patterns such as `v4.x` or `v4.5.x`.
- Prefer the latest milestone that looks like a concrete release version such as `v4.5.0`.
- If a valid milestone exists, ask: `Use <MILESTONE_VERSION> as the release version? (yes/no)`.
- If the answer is yes, use that version.
- If the answer is no, ask the user to provide the release version explicitly.
- If no valid milestone exists, ask the user to provide the release version explicitly.

1. Confirm release version:

- Ensure a final release version is confirmed from either:
  - Milestone confirmation (yes), or
  - User-provided version.

1. Check whether the tag already exists:

```bash
gh api repos/{owner}/{repo}/git/ref/tags/<RELEASE_VERSION>
```

If the tag already exists, proceed only if that is intentional for the repository workflow.

1. Check whether a GitHub release already exists for the selected version:

```bash
gh api repos/{owner}/{repo}/releases/tags/<RELEASE_VERSION>
```

Decision rules:

- If the release does not exist, create it.
- If the release exists, inspect whether it is draft, prerelease, or already a full release.
- If the user selected `prerelease`, update the existing release to keep or set `prerelease=true` as needed.
- If the user selected `release`, update the existing release to set `prerelease=false`.
- Do not attempt `gh release create` again when a release already exists for the tag.

1. When the user selected `release`, ensure the announcement discussion is linked:

- Prefer the repository discussion category named `Announcements`.
- If needed, inspect repository discussion categories before updating the release.
- When creating a release, use `discussion_category_name` with `Announcements`.
- When updating an existing release, use the release update API with `discussion_category_name=Announcements`.
- If a discussion is already linked, preserve it.

1. Create the release:

Prerelease:

```bash
gh release create <RELEASE_VERSION> --repo {owner}/{repo} --title '<RELEASE_VERSION>' --prerelease
```

Release:

```bash
gh api repos/{owner}/{repo}/releases -X POST -f tag_name='<RELEASE_VERSION>' -f name='<RELEASE_VERSION>' -F prerelease=false -f discussion_category_name='Announcements'
```

Update existing release to full release and link announcement discussion:

```bash
gh api repos/{owner}/{repo}/releases/<RELEASE_ID> -X PATCH -F prerelease=false -F make_latest=true -f discussion_category_name='Announcements'
```

1. Report:

- Release version
- Release type
- Whether a valid milestone version was found
- Whether the milestone version was confirmed (yes/no)
- Whether the version was milestone-confirmed or user-provided
- Whether the tag already existed
- Whether a GitHub release already existed for the version
- Whether the GitHub release was created or updated successfully
- Whether the release ended as prerelease or full release
- Whether the announcement discussion was linked
