---
name: release
description: Guided GitVersion release workflow — analyzes unreleased changes, checks release readiness (milestone, labels), creates the GitHub release, verifies published artifacts, and monitors downstream PRs (Homebrew, winget, GitTools Actions).
allowed-tools:
  - Bash(command -v gh)
  - Bash(gh --version)
  - Bash(gh auth status)
  - Bash(git log *)
  - Bash(git tag *)
  - Bash(git describe *)
  - Bash(git status *)
  - Bash(git fetch *)
  - Bash(git rev-list *)
  - Bash(gh release *)
  - Bash(gh repo sync *)
  - Bash(gh api *)
  - Bash(gh pr list *)
  - Bash(gh pr view *)
  - Bash(gh milestone *)
  - Bash(python3 *)
  - Bash(python3 -c *)
  - Bash(curl *)
  - Read(GitReleaseManager.yml)
  - AskUserQuestion
---

# Release Skill — GitVersion Release Workflow

You are helping the maintainer of GitVersion prepare and execute a release. Work through the phases below in order. Never skip a phase. Always show your findings before moving to the next phase.

---

## Phase 0 — Prerequisites

Check that the GitHub CLI is installed and authenticated before doing anything else:

```bash
command -v gh >/dev/null && gh --version || echo "MISSING"
gh auth status
gh auth status --hostname github.com   # must be logged in to github.com specifically
```

If `gh` is missing, stop and tell the user to install it (e.g. `brew install gh` on macOS, or https://cli.github.com/) and run `gh auth login`, then re-run this skill. Do not proceed to Phase 1 until `gh` is present and authenticated.

### Local release branch is safe

Releases must be cut from a clean, current `main` checkout. Run this once and show
the result in the preflight table:

```bash
git status --short
git branch --show-current
git fetch origin main
git rev-list --left-right --count HEAD...origin/main
```

- Require an empty `git status --short`, branch `main`, and `0 0` from `rev-list`.
- If local changes exist, stop: do not mix release work with unrelated edits.
- If the branch is behind, fast-forward it before continuing. If it is ahead, stop
  and ask the user whether those commits are intended for this release.

### Homebrew fork is current

The Homebrew publish workflow uses the `gittools-bot/homebrew-core` fork to open its
formula PR. GitHub rejects or cannot reliably create that PR when the fork is behind
`Homebrew/homebrew-core`, so check it **before starting the release**, rather than
discovering it after the CI pipeline has published all other artifacts.

```bash
# Confirm the expected fork and obtain the upstream's default branch.
gh api repos/gittools-bot/homebrew-core --jq \
  '{fork, default_branch, parent: {full_name: .parent.full_name, default_branch: .parent.default_branch}}'

# `behind_by` must be zero. `ahead_by` may be non-zero if the fork has local PR branches.
BRANCH=$(gh api repos/Homebrew/homebrew-core --jq '.default_branch')
gh api "repos/Homebrew/homebrew-core/compare/${BRANCH}...gittools-bot:${BRANCH}" --jq \
  '{status, ahead_by, behind_by, html_url}'
```

- `behind_by: 0` → ✅ the fork is ready.
- `behind_by: >0` → ❌ **Blocker:** `gittools-bot/homebrew-core` is behind upstream.
  Ask the user whether to update it with GitHub CLI, then run:

  ```bash
  gh repo sync gittools-bot/homebrew-core \
    --source Homebrew/homebrew-core \
    --branch "$BRANCH"
  ```

  This uses a fast-forward update and must be preferred over `--force`; do not use
  `--force` unless the user explicitly authorizes replacing the fork's default
  branch. Re-run the comparison afterwards. Do not proceed to Phase 1 until it
  reports `behind_by: 0`.
- If the fork is missing or is not a fork of `Homebrew/homebrew-core`, treat that as
  a blocker and ask the user to restore/correct the bot fork before proceeding.

---

## Phase 1 — Analyse unreleased changes

Run these commands in parallel:

```bash
# Last stable release
gh release list --repo GitTools/GitVersion --limit 1 --json tagName,publishedAt --jq '.[0]'

# Commits since last release (non-merge)
git log $(git describe --tags --abbrev=0)..HEAD --no-merges --oneline

# Merge commits (= merged PRs) since last release
git log $(git describe --tags --abbrev=0)..HEAD --merges --oneline
```

Then fetch labels for every merged PR number you found above:

```bash
# Replace NUMBERS with the PR numbers extracted from merge commit messages (e.g. #4972 → 4972)
gh pr list --repo GitTools/GitVersion --state merged --json number,title,labels \
  --search "$(git log $(git describe --tags --abbrev=0)..HEAD --merges --format='%s' | grep -oE '#[0-9]+' | tr '\n' ' ')"
```

**Classify the changes:**

| Signal | Suggested bump |
|---|---|
| Any PR/commit with label `breaking change` or message containing `BREAKING` | **major** |
| Any PR/commit with label `feature` or prefix `feat:` | **minor** |
| Only `bug`, `improvement`, `documentation`, `dependencies`, `build` | **patch** |

Present a concise summary: how many PRs, what categories, the breaking/feature/patch breakdown, and your suggested bump type.

---

## Phase 2 — Ask for release type

Use `AskUserQuestion` to confirm the release type with the user. Present the suggestion from Phase 1 as the first (recommended) option. Offer all three bump types plus pre-release.

Once the user answers, compute the new version:

```bash
LAST_TAG=$(git describe --tags --abbrev=0)
# Parse MAJOR.MINOR.PATCH from $LAST_TAG and bump according to user choice
python3 -c "
import re, sys
tag = '$LAST_TAG'.lstrip('v')
# strip any pre-release suffix
base = re.split(r'[-+]', tag)[0]
parts = base.split('.')
major, minor, patch = int(parts[0]), int(parts[1]), int(parts[2])
bump = sys.argv[1]  # major | minor | patch
if bump == 'major': print(f'{major+1}.0.0')
elif bump == 'minor': print(f'{major}.{minor+1}.0')
else: print(f'{major}.{minor}.{patch+1}')
" <BUMP_TYPE>
```

State the computed version clearly, e.g. **Next release: `6.8.0`**.

---

## Phase 3 — Readiness checklist

Before querying milestones, confirm that the target does not already have a GitHub
release. An existing release is a hard stop unless the user explicitly asks to
repair or update that release:

```bash
gh release view <VERSION> --repo GitTools/GitVersion --json url,isDraft
```

### Consolidate safe fixes

Run every read-only readiness check first. Present one compact table and one
numbered list of proposed changes, then ask for **one** confirmation to apply the
approved safe actions together (for example: create the milestone, move closed
items from the umbrella milestone, and assign clearly-labelled untracked PRs).
Do not silently choose a label for an unlabelled or multiply-labelled item: list the
recommended label and require the user's explicit approval for that semantic choice.
After applying any moves or assignments, perform the milestone-index settle check
before entering Phase 4.

Run all four checks in parallel. Collect all results before presenting anything to the user — show a single consolidated report, not incremental output for each check.

### 3a. Milestone exists

```bash
# Fetch all open milestones to find both the exact target and any related umbrella milestones
gh api repos/GitTools/GitVersion/milestones --jq \
  '[.[] | {title, number, state, open_issues, closed_issues}]'
```

From the result:
- Look for an **exact match** on `<VERSION>` (e.g. `6.8.0`). If found, record its milestone number for use in 3b/3c/3d.
- Also look for **related umbrella milestones**: milestones whose title shares the same major version prefix (e.g. `6.x`, `6.8`, `6.8.x`). Record any found for use in 3a-fix below.

**If the milestone is missing — blocker 3a:**

> ❌ **Blocker:** Milestone `<VERSION>` does not exist.
>
> Ask the user via `AskUserQuestion`: *"Should I create the `<VERSION>` milestone on GitHub now?"*
>
> If yes:
> ```bash
> gh api repos/GitTools/GitVersion/milestones \
>   --method POST \
>   --field title="<VERSION>" \
>   --field state="open"
> ```
> Record the new milestone number. Then immediately run check 3a-fix below.
>
> If no: tell the user to create it manually at https://github.com/GitTools/GitVersion/milestones/new and pause.

**3a-fix — Move closed items from related umbrella milestones:**

If any related umbrella milestones exist (e.g. `6.x`), fetch their closed items:

```bash
# --paginate streams every page (a milestone with >100 closed items would otherwise truncate to the first page)
gh api "repos/GitTools/GitVersion/issues?milestone=<UMBRELLA_MILESTONE_NUMBER>&state=closed&per_page=100" --paginate \
  --jq '.[] | {number, title, labels: [.labels[].name]}'
```

If closed items exist in the umbrella milestone(s), ask the user via `AskUserQuestion`:
*"Found N closed items in `<UMBRELLA_MILESTONE>`. Should I move them to `<VERSION>`?"*

If yes, move each item:
```bash
# Repeat for each issue/PR number from the umbrella milestone
gh api repos/GitTools/GitVersion/issues/<NUMBER> \
  --method PATCH \
  --field milestone=<NEW_MILESTONE_NUMBER>
```

Confirm how many were moved.

### 3b. Open issues/PRs in milestone

```bash
gh api "repos/GitTools/GitVersion/issues?milestone=<MILESTONE_NUMBER>&state=open&per_page=100" --paginate \
  --jq '.[] | {number, title, url: ("https://github.com/GitTools/GitVersion/issues/" + (.number | tostring)), type: (if .pull_request then "PR" else "issue" end)}'
```

**If open items exist — warning 3b:**

> ⚠️ **Warning:** N open items remain in the `<VERSION>` milestone. These will not be included in the release notes.
>
> List each item as: `- [#NUMBER](url) type: TITLE`
>
> Suggested actions:
> - Close the item if the work is done
> - Move it to a future milestone with: `gh api repos/GitTools/GitVersion/issues/<NUMBER> --method PATCH --field milestone=<OTHER_NUMBER>`
> - Leave it (it will stay open and carry over)
>
> Ask the user: *"How do you want to handle these open items — move them, or proceed anyway?"*
> Offer: "Move to next milestone", "Leave them and proceed", "I'll handle it manually (pause)"

### 3c. Closed items in milestone without a valid GitReleaseManager label

First, read the label configuration from `GitReleaseManager.yml` in the repo root:

```bash
cat GitReleaseManager.yml
```

Extract:
- **included labels** (`issue-labels-include`): labels that cause an item to appear in release notes — `breaking change`, `bug`, `dependencies`, `documentation`, `feature`, `improvement`
- **excluded labels** (`issue-labels-exclude`): labels that explicitly hide an item from release notes — `build`

Then fetch closed milestone items and classify each one:

```bash
gh api "repos/GitTools/GitVersion/issues?milestone=<MILESTONE_NUMBER>&state=closed&per_page=100" --paginate \
  --jq '.[] | {number, title, url: ("https://github.com/GitTools/GitVersion/issues/" + (.number | tostring)), labels: [.labels[].name]}'
```

For a fast offender scan, this one-pass filter prints only the items whose count of GRM-relevant labels (the six included + the excluded `build`) is not exactly one — i.e. every zero-label, multi-label, and mixed-with-`build` case in a single line each:

```bash
gh api "repos/GitTools/GitVersion/issues?milestone=<MILESTONE_NUMBER>&state=closed&per_page=100" --paginate \
  --jq '.[] | {n:.number, type:(if .pull_request then "PR" else "issue" end), labels:[.labels[].name]}
        | (.labels | map(select(. as $l | ["breaking change","bug","dependencies","documentation","feature","improvement","build"] | index($l)))) as $valid
        | select(($valid|length) != 1)
        | "\(.type) #\(.n)\tvalid=\($valid)\tall=\(.labels)"'
```

An empty result means every closed item has exactly one GRM-relevant label. For anything it prints, classify it with the full logic below (the scan can't tell an included label from `build`, so a lone `build` shows up here as a ⚠️, not a ❌). For each item, count how many of its labels match `issue-labels-include`, then apply this logic (GitReleaseManager, hereafter GRM, requires **exactly one** included label per item — it categorises each item under a single section and aborts if an item has more than one matching label):

- **Exactly one `issue-labels-include` label** → ✅ will appear in release notes under that label's section
- **Two or more `issue-labels-include` labels** (e.g. `bug` + `improvement`, or `bug` + `breaking change`) → ❌ blocker: GRM supports only one label per item and will fail because it cannot decide which section to file it under
- **No labels at all** → ❌ blocker: GRM will fail (no section to file it under)
- **Has only `issue-labels-exclude` labels** (e.g. only `build`) → ⚠️ warning: will be silently skipped in release notes (not a GRM failure, but intentional exclusion — flag it so the user can confirm it's deliberate)
- **Has labels but none from either list** (e.g. only `good first issue`) → ❌ blocker: GRM will fail because no included label matches

**If any items fall into a blocker category — blocker 3c:**

> ❌ **Blocker:** N closed items in the milestone will cause GitReleaseManager to fail.
>
> List each blocker item as: `- [#NUMBER](url) TITLE  current labels: [label1, label2] (or none) — reason: <none | multiple included labels | no matching label>`
>
> **For items with no matching label or no labels:** apply exactly one of the valid `issue-labels-include` labels from `GitReleaseManager.yml`:
> `breaking change`, `bug`, `dependencies`, `documentation`, `feature`, `improvement`
>
> ```bash
> gh api repos/GitTools/GitVersion/issues/<NUMBER>/labels \
>   --method POST --field 'labels[]=<LABEL>'
> ```
>
> **For items with multiple included labels:** remove all but the single most appropriate one (precedence: `breaking change` > `feature` > `bug` > `improvement` > `dependencies` > `documentation`):
>
> ```bash
> gh api repos/GitTools/GitVersion/issues/<NUMBER>/labels/<LABEL_TO_REMOVE> \
>   --method DELETE
> ```
>
> Also list any ⚠️ items with only excluded labels separately so the user can confirm those exclusions are intentional.
>
> Tell the user to fix the blockers, then confirm they are done before continuing.

### 3c-bis. At least one closed issue present

GitReleaseManager needs at least one closed **issue** (not just PRs) in the milestone. With a PR-only milestone GRM can fail to generate notes or produce an empty/degenerate release, so verify an issue is present:

```bash
gh api "repos/GitTools/GitVersion/issues?milestone=<MILESTONE_NUMBER>&state=closed&per_page=100" --paginate \
  --jq '[.[] | select(.pull_request | not)] | length'
```

- **≥ 1** → ✅ proceed.
- **0** (milestone contains only PRs) → ❌ blocker: ensure the work is tracked by at least one closed issue assigned to the milestone (create/label a tracking issue, or attach the relevant issue), then re-check.

### 3d. Merged PRs not assigned to milestone

```bash
# Step 1: get all item numbers currently in the milestone
# --paginate --slurp so milestones with >100 items aren't truncated to the first page (--slurp can't combine with --jq, so flatten the page arrays in python)
MILESTONE_ITEMS=$(gh api "repos/GitTools/GitVersion/issues?milestone=<MILESTONE_NUMBER>&state=closed&per_page=100" --paginate --slurp \
  | python3 -c "import sys, json; pages = json.load(sys.stdin); print(json.dumps([i['number'] for pg in pages for i in pg]))")

# Step 2: get PRs merged since last release
# IMPORTANT: pass --limit well above gh's default of 30, or merged PRs will be silently dropped from this check
LAST_DATE=$(gh release list --repo GitTools/GitVersion --limit 1 --json publishedAt --jq '.[0].publishedAt')
gh pr list --repo GitTools/GitVersion --state merged --base main \
  --search "merged:>=${LAST_DATE}" --limit 200 --json number,title,labels \
  --jq "[.[] | select(.number as \$n | ($MILESTONE_ITEMS | index(\$n)) == null) | {number, title, url: (\"https://github.com/GitTools/GitVersion/pull/\" + (.number | tostring)), labels: [.labels[].name]}]"
```

For each unassigned PR found, check whether it **closes a linked issue** — if the issue (not the PR) already carries the milestone/label, adding the PR too would double-count the change in release notes:

```bash
# Repeat for each PR number found above
gh pr view <NUMBER> --repo GitTools/GitVersion --json closingIssuesReferences --jq '[.closingIssuesReferences[]?.number]'
```

- **Closes an issue** → do NOT add the PR to the milestone (the linked issue should already be in it, or needs to be — that's where the change is tracked). Skip it from the warning list below.
- **Does not close an issue** (the PR itself is the unit of change — e.g. dependency bumps, CI/build fixes with no tracking issue) → it belongs in the milestone.

**If PRs that don't close an issue remain unassigned — warning 3d:**

> ⚠️ **Warning:** N merged PRs (that don't close a tracked issue) are not assigned to the `<VERSION>` milestone and won't appear in release notes.
>
> List each as: `- [#NUMBER](url) TITLE  labels: [label1, label2]`
> Note if any are also unlabelled (compound issue with 3c) — apply one `issue-labels-include` label (e.g. `improvement` for CI/build hygiene PRs with no clearer fit) before assigning, or GRM will fail on it.
>
> Suggested action — label (if needed) and assign each to the milestone:
> ```bash
> gh api repos/GitTools/GitVersion/issues/<NUMBER>/labels --method POST --field 'labels[]=<LABEL>'
> gh api repos/GitTools/GitVersion/issues/<NUMBER> \
>   --method PATCH --field milestone=<MILESTONE_NUMBER>
> ```
>
> Ask the user: *"Should I assign all N unassigned merged PRs to the `<VERSION>` milestone now?"*
> If yes, run the command for each PR and confirm.

### 3d-bis. In-milestone PRs that close an in-milestone issue (double-count)

The inverse of 3d: a PR **already in** the milestone that closes an issue **also in** the milestone gets counted twice in the release notes (once as the PR, once as the issue). For each PR in the milestone, check its closing references against `$MILESTONE_ITEMS` (captured in 3d). The GraphQL form below is more reliable than `gh pr view` when the closing reference spans repos:

```bash
# For a suspect PR already in the milestone, is its closing target also in the milestone?
gh api graphql -f query='query($o:String!,$r:String!,$n:Int!){repository(owner:$o,name:$r){pullRequest(number:$n){closingIssuesReferences(first:20){nodes{number}}}}}' \
  -F o=GitTools -F r=GitVersion -F n=<PR_NUMBER> \
  --jq '[.data.repository.pullRequest.closingIssuesReferences.nodes[].number]'
```

**If any in-milestone PR closes an in-milestone issue — blocker 3d-bis:**

> ❌ **Blocker:** N PRs in the milestone close an issue that is also in the milestone — the change would be double-counted in the release notes.
>
> List each as: `- PR [#NUMBER](url) closes issue #ISSUE (both in milestone)`
>
> Fix — drop the PR from the milestone (the linked issue is the tracked unit of change):
> ```bash
> gh api repos/GitTools/GitVersion/issues/<PR_NUMBER> --method PATCH -f milestone=null
> ```
>
> Ask the user before removing. The issue stays in the milestone and carries the change in the notes.

### Present the consolidated checklist

After all checks complete and any fixes have been applied, show a single readiness summary:

```
Release Readiness: <VERSION>
─────────────────────────────────────────────────────
✅/❌  Milestone <VERSION> exists (N open, N closed items)
✅/⚠️  Open items in milestone: N
✅/❌  At least one closed issue present (3c-bis)
✅/❌/⚠️  Closed items have exactly one GRM label  (N blockers, N multi-label, N excluded-only)
✅/❌  No in-milestone PR closes an in-milestone issue (3d-bis)
✅/⚠️  All merged PRs in milestone (N missing)
```

**Hard blockers (❌)** — do not proceed to Phase 4 until resolved:
- Milestone does not exist (and user declined to create it)
- No closed issue in the milestone (PR-only milestone — 3c-bis)
- Closed items with no labels, or whose only labels don't match any `issue-labels-include` entry in `GitReleaseManager.yml`
- Closed items with **more than one** `issue-labels-include` label (GRM supports only one label per item)
- An in-milestone PR closes an in-milestone issue (double-count — 3d-bis)

**Warnings (⚠️)** — ask the user to confirm before proceeding to Phase 4:
- Open items remaining in the milestone
- Merged PRs not yet assigned to the milestone

### ⚠️ Milestone-index settle check (prevents PRs vanishing from release notes)

**Why this matters:** GitReleaseManager fetches milestone items via the REST `GET /issues?milestone=N&state=closed` listing, which is backed by GitHub's milestone *index*. When items are **bulk-moved** into the milestone (e.g. the 3a-fix move from a `6.x` umbrella, or late 3d assignments), that index lags — sometimes by many minutes. If GRM runs before it catches up, the just-moved items (overwhelmingly PRs) are silently absent from the listing and **dropped from the release notes**, even though they're correctly assigned. This is exactly how 6.6.1 through 6.8.0 each lost *all* their PRs (e.g. 6.8.0 rendered 2 of 86 items) while 6.6.0 — whose PRs were milestoned at merge time — was complete.

After any move/assignment in 3a-fix or 3d, verify the index reflects what you just changed before proceeding:

```bash
# The count the listing endpoint returns must equal the milestone's own closed-item count.
# --paginate --slurp to count across every page (>100 items); --slurp can't combine with --jq, so sum the page lengths in python.
LISTED=$(gh api "repos/GitTools/GitVersion/issues?milestone=<MILESTONE_NUMBER>&state=closed&per_page=100" --paginate --slurp \
  | python3 -c "import sys, json; print(sum(len(pg) for pg in json.load(sys.stdin)))")
MILESTONE_CLOSED=$(gh api repos/GitTools/GitVersion/milestones/<MILESTONE_NUMBER> --jq '.closed_issues')
echo "listed=$LISTED  milestone.closed_issues=$MILESTONE_CLOSED"
```

- If `LISTED == MILESTONE_CLOSED` → ✅ index is consistent; safe to proceed.
- If `LISTED < MILESTONE_CLOSED` → ⚠️ index still settling. Wait ~30–60s and re-check; do **not** create the release (Phase 4) until they match, or GRM will miss the lagging items.

Prefer assigning items to the target milestone **at merge time** over bulk-moving from an umbrella milestone right before release — that avoids the lag window entirely.

At the end of Phase 3, print only the consolidated readiness table, the actions
applied, and any remaining blocker/warning. Avoid replaying raw API output.

---

## Phase 4 — Create the GitHub Release

Only proceed after the user explicitly confirms readiness.

```bash
gh release create <VERSION> \
  --repo GitTools/GitVersion \
  --title "<VERSION>" \
  --notes "" \
  --discussion-category "Announcements" \
  --target main
```

> **Announcements discussion:** `--discussion-category "Announcements"` links the release to a discussion in the repo's **Announcements** category so users can follow up in one place. The category must already exist (it does — verify with `gh api graphql -f query='{repository(owner:"GitTools",name:"GitVersion"){discussionCategories(first:20){nodes{name}}}}'` if unsure). Omit the flag only if the user explicitly doesn't want a discussion for this release.

> **Important:** Do NOT mark it as a pre-release unless the version contains a pre-release label (e.g. `-beta.1`). Check automatically:
> ```bash
> python3 -c "import sys; tag=sys.argv[1]; print('--prerelease' if '-' in tag.split('+')[0] else '')" <VERSION>
> ```

After running, confirm the release URL was created and inform the user that the CI pipeline is now running (`ci-release` dispatch was triggered by the release publication).

Provide the release URL: `https://github.com/GitTools/GitVersion/releases/tag/<VERSION>` — and the linked discussion URL:
```bash
gh api repos/GitTools/GitVersion/releases/tags/<VERSION> --jq '.discussion_url'
```

---

## Phase 5 — Monitor downstream PRs

Tell the user: the CI pipeline triggered by the release takes 20–40 minutes to complete, after which it fires a `publish-release` dispatch to Homebrew, winget, and GitTools Actions. You will monitor all three automatically using `/loop`.

**"Not yet created" is an ambiguous status — checking PR existence alone does not disambiguate it.** It could mean the publish CI is still running, *or* that the publish workflow already ran and failed. A loop that only searches for PRs will report "not yet created" for the full 40-minute window even when the underlying job failed minutes in. Always cross-check the actual workflow run conclusion alongside the PR search; do not rely on elapsed time alone to infer failure.

> **Note on GitTools Actions:** all three downstream targets — Homebrew, winget, and GitTools Actions — open a PR that must be verified. If you find a *recent direct commit to `main`* on `GitTools/actions` for this version but no PR, treat that as unexpected and flag it — don't record it as a normal terminal state.

**Scope of monitoring: PR creation only, not merge status.** Once a target has a confirmed PR (any state — `OPEN` is sufficient), mark that target resolved: record its link and exclude it from subsequent polling iterations. A downstream job that explicitly reports a no-op (for example `pull-request-operation = none`) is also resolved; report that no PR was needed. Merge status is outside this skill's scope — it depends on third-party reviewers and is not deterministic on any timeline this loop should poll against. Continue polling only the targets that remain unresolved (no PR, no explicit no-op, and no failure signal yet).

Invoke the `loop` skill via the Skill tool with the following self-paced prompt (substitute the actual version for `<VERSION>`):

```
Skill({
  skill: "loop",
  args: "Check downstream release PRs for GitVersion <VERSION>. Only check targets that haven't already resolved (exclude a target from the checks below once it has a confirmed PR/commit — see stop condition). Run these checks in parallel for unresolved targets:
    1. gh pr list --repo Homebrew/homebrew-core --search \"gitversion <VERSION>\" --state all --json number,title,state,url --jq '.[]'
    2. gh pr list --repo microsoft/winget-pkgs --search \"GitTools.GitVersion version <VERSION>\" --state all --json number,title,state,url --jq '.[]'
    3. gh pr list --repo GitTools/actions --search \"gitversion <VERSION>\" --state all --limit 5 --json number,title,state,url --jq '.[]'
    4. For each target with no PR found yet, also check whether its publish workflow already ran and what it concluded — don't just wait blindly:
       gh run list --repo GitTools/GitVersion --limit 30 --json name,event,status,conclusion,createdAt --jq '[.[] | select(.name == \"Publish to Homebrew\" or .name == \"Publish to Winget\" or .name == \"Update GitTools Actions\")]'
    5. If GitTools Actions has no PR but its workflow run succeeded, sanity-check that it didn't push directly to main instead of opening a PR:
       gh api repos/GitTools/actions/commits?per_page=5 --jq '.[] | {sha, message: .commit.message, date: .commit.author.date}'
  Display a status table:
    Downstream Release Status: <VERSION>
    ─────────────────────────────────────────────────────
    📦 Homebrew          <url> / not yet created (CI still running) / FAILED: <run url>
    📦 winget            <url> / not yet created (CI still running) / FAILED: <run url>
    ⚙️  GitTools Actions  <url> / no PR needed (confirmed no-op) / not yet created (CI still running) / FAILED: <run url> / ⚠️ pushed directly to main (unexpected): <commit url>
  Self-pace: use a long fallback (1200s) while CI is still running and nothing has resolved yet.
  If a publish workflow run shows conclusion 'failure', surface it to the user immediately in the next message rather than waiting out the fallback — don't auto-retry a failed publish workflow without asking, since it's a one-way external action (opens a PR/pushes to a third-party repo).
  Stop the loop (omit ScheduleWakeup) once every target has reached a terminal state: PR confirmed created (any state, don't track to merge), no-op confirmed from its downstream job output (for example `pull-request-operation = none`), or FAILED-and-reported. A GitTools Actions direct-push-to-main is not a normal terminal state — report it as unexpected (see note above) rather than silently resolving the target on it."
})
```

The loop self-paces as follows:
- While CI is still running and no target has resolved → long fallback interval (~20 min)
- Once a target's PR or explicit no-op is confirmed, that target is excluded from further polling
- A failed publish workflow run is reported in the next message, not deferred until the fallback interval elapses
- The loop terminates once every target has reached a terminal state (PR confirmed, no-op confirmed, or failure reported)

**Homebrew publish mechanism:** `.github/workflows/homebrew.yml` runs Homebrew's own `brew bump-formula-pr` CLI. It computes the source tarball SHA-256, forks via the `gittools-bot` org, and commits as the GitTools Bot identity, opening a PR to `Homebrew/homebrew-core`. If the job fails, investigate the actual `brew bump-formula-pr` step output — likely causes are the `HOMEBREW_GITHUB_API_TOKEN` lacking fork/push scope, the `gittools-bot` fork being out of sync, or `brew bump-formula-pr` rejecting the formula (audit/version). Surface the failing run's log to the user.

---

## Phase 6 — Verify the release artifacts

Run once the `ci-release` pipeline (triggered by the release publish) has completed — check with:

```bash
gh run list --repo GitTools/GitVersion --limit 30 --json databaseId,name,event,status,conclusion,createdAt \
  --jq '[.[] | select(.event == "repository_dispatch" or .name == "Release")]'
```

Then run these checks in parallel and present a single consolidated report (don't trickle results):

### 6a. NuGet packages published

Only these three packages are published (confirmed via `<PackageId>` in their `.csproj` files — don't assume others like `GitVersion.App`/`GitVersion.Core.Tests` are published):

```bash
for pkg in gitversion.tool gitversion.core gitversion.msbuild; do
  curl -s "https://api.nuget.org/v3-flatcontainer/$pkg/index.json" | python3 -c "
import sys, json
d = json.load(sys.stdin)
versions = d.get('versions', [])
print('$pkg', '<VERSION>' in versions, '(latest:', versions[-1] if versions else 'NONE', ')')
"
done
```

✅ if `<VERSION>` appears in each package's version list.

### 6b. Docker images available + README reflects the release

Query Docker Hub's public API directly (do NOT infer from CI job success alone — a green "Docker Images" CI job confirms the push *attempt* succeeded, not that the tag is visible on the registry):

```bash
curl -s "https://hub.docker.com/v2/repositories/gittools/gitversion/tags?page_size=100" | python3 -c "
import sys, json
d = json.load(sys.stdin)
tags = [t['name'] for t in d.get('results', [])]
matching = [t for t in tags if t.startswith('<VERSION>')]
print(f'{len(matching)} tags found for <VERSION>')
print(matching[:10])
"

curl -s "https://hub.docker.com/v2/repositories/gittools/gitversion/" | python3 -c "
import sys, json, re
d = json.load(sys.stdin)
desc = d.get('full_description', '')
print('<VERSION>' in desc, 'mentions <VERSION>')
print(sorted(set(re.findall(r'\d+\.\d+\.\d+', desc))))
"
```

✅ if tags exist for `<VERSION>` across the expected OS/runtime variants, and the README's `full_description` references `<VERSION>` (not a stale older version).

### 6c. Chocolatey package status

```bash
python3 -c "
import urllib.request, urllib.parse
filt = \"(Id eq 'GitVersion.Portable') and (Version eq '<VERSION>')\"
url = 'https://community.chocolatey.org/api/v2/Packages()?\$filter=' + urllib.parse.quote(filt)
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0', 'Accept': 'application/atom+xml'})
data = urllib.request.urlopen(req).read().decode()
print('Published' in data and '1900-01-01' not in data, '<- True means publicly approved')
print('entry found' if '<VERSION>' in data else 'NOT FOUND')
"
```

- If the package isn't found at all: ❌ blocker — the Chocolatey push step in CI may have failed; check the `Publish / Chocolatey` job log.
- If found but `Published` is `1900-01-01T00:00:00`: ⚠️ expected behavior — the Chocolatey Community Repository queues new package versions for moderation before they become publicly visible as "latest." This is not an error and requires no remediation.

### 6d. Release notes are accurate

```bash
gh release view <VERSION> --repo GitTools/GitVersion --json body --jq '.body'
```

Sanity-check: body is non-empty, lists categorized changes (Breaking change / Features / Bugs / Improvements / Dependencies per `GitReleaseManager.yml` aliases), and includes contributors. GitReleaseManager lists **every** milestone item with a single included label — issues *and* PRs alike (dependency-bump PRs labelled `dependencies` get their own `Dependencies` section). A large milestone should produce correspondingly large notes; sparse notes against a full milestone is a **red flag**, not normal. GitHub release assets already expose their SHA-256 digests; do not add a hash section to the release notes.

**Reconcile the rendered item count against the milestone (catches the dropped-PR failure):**

```bash
# Items GRM should have rendered = closed milestone items minus the excluded-label (build) ones.
# --paginate --slurp to include every page (>100 items); --slurp can't combine with --jq, so filter/count in python.
EXPECTED=$(gh api "repos/GitTools/GitVersion/issues?milestone=<MILESTONE_NUMBER>&state=closed&per_page=100" --paginate --slurp \
  | python3 -c "import sys, json; pages = json.load(sys.stdin); print(sum(1 for pg in pages for i in pg if 'build' not in [l['name'] for l in i['labels']]))")
# Items GRM actually rendered = distinct #NNNN / !NNNN references in the published body.
RENDERED=$(gh release view <VERSION> --repo GitTools/GitVersion --json body --jq '.body' \
  | grep -oE '[#!][0-9]+' | sort -u | wc -l | tr -d ' ')
echo "expected≈$EXPECTED  rendered=$RENDERED"
```

- If `RENDERED` is roughly `EXPECTED` → ✅ notes are complete.
- If `RENDERED` is far below `EXPECTED` (e.g. 2 vs 86) → ❌ GRM ran against a stale milestone index and dropped items (see the Phase 3 settle check). **Fix:** the milestone listing is now consistent, so regenerate the notes in place — `dotnet tool restore && dotnet gitreleasemanager create -m <VERSION> -o GitTools -r GitVersion --token "$(gh auth token)"` (`allow-update-to-published: true` in `GitReleaseManager.yml` lets it update the published release). Then re-run this reconciliation to confirm.

### 6e. Docs schema published (major/minor releases only)

Skip this check entirely for patch releases. For major or minor releases, the schema URL uses **`Major.Minor` only** (e.g. `6.8`, never the full `6.8.0` patch version — confirmed from `build/docs/Tasks/GenerateSchemas.cs`, which generates `$"{gitVersion.Major}.{gitVersion.Minor}"`):

```bash
MAJOR_MINOR=$(echo "<VERSION>" | cut -d. -f1,2)
for f in GitVersion.json GitVersion.configuration.json; do
  curl -s -o /dev/null -w "%{http_code} $f\n" "https://gitversion.net/schemas/${MAJOR_MINOR}/$f"
done
```

✅ both return `200`. If either 404s, check that the "Verify & Publish Docs" `repository_dispatch` workflow run completed successfully:

```bash
gh run list --repo GitTools/GitVersion --limit 30 --json name,event,status,conclusion,createdAt \
  --jq '[.[] | select(.name == "Verify & Publish Docs")] | .[0]'
```

If that workflow didn't run or failed, that's the blocker — re-dispatch or investigate it rather than the schema URL itself.

### 6f. Announcements discussion linked

Confirm the release is linked to a discussion in the **Announcements** category (created via `--discussion-category` in Phase 4):

```bash
gh api repos/GitTools/GitVersion/releases/tags/<VERSION> --jq '.discussion_url'
```

- Non-null URL → ✅ discussion linked; include it in the summary.
- `null` → ⚠️ no discussion linked (the `--discussion-category` flag was omitted or the category name didn't match). Not a hard blocker, but flag it so the user can create one manually if intended.

### Present the verification report

```
Release Verification: <VERSION>
─────────────────────────────────────────────────────
✅/❌/⏳  NuGet (GitVersion.Tool, .Core, .MsBuild)
✅/❌/⏳  Docker images + README
✅/⚠️/❌  Chocolatey (pushed / pending moderation / missing)
✅/⚠️  Release notes accurate
✅/❌/➖/⏳  Docs schema at /schemas/<major.minor>/ (➖ = skipped, patch release)
✅/⚠️  Announcements discussion linked
```

⏳ means the underlying CI/publish step already succeeded but the artifact isn't visible yet (NuGet search-index lag, docs CDN propagation) — this is expected to clear within minutes, not a failure. Distinguish it from ❌ (the publish step itself failed or never ran) by checking the relevant CI job's `conclusion` first.

### If anything is still ⏳ — retry on a loop instead of polling manually

NuGet's package index and the gitversion.net docs CDN can both lag a few minutes behind a successful publish. Rather than re-running checks by hand, invoke the `loop` skill via the Skill tool with a self-paced prompt (substitute `<VERSION>`):

```
Skill({
  skill: "loop",
  args: "Re-run release verification checks for GitVersion <VERSION> that were still ⏳ (pending indexing/propagation): [list only the still-pending checks from 6a/6b/6e — don't re-check ones that already passed].
  Self-pace: short delay (120-180s) since these are indexing/CDN-propagation delays, not long-running CI — they typically clear in a few minutes.
  After 5 consecutive attempts still pending, stop the loop and report to the user that propagation is taking longer than expected, with a link to the underlying publish job so they can check it directly rather than continuing to poll indefinitely.
  Stop the loop (omit ScheduleWakeup) once all previously-⏳ checks pass, and show the final consolidated verification report."
})
```

Flag any genuine ❌ to the user immediately with the specific failing check and the relevant CI run/log to investigate — don't silently retry publish steps without asking, since each one (NuGet push, Docker push, Chocolatey push) is a one-way external action. Only the read-only verification checks (6a/6b/6e) are safe to retry automatically in the loop; never re-trigger a publish workflow without confirming with the user first.

---

## Phase 7 — Final summary

Once Phase 6 verification is done (or has handed off to a loop), close out with a summary the maintainer can scan later without re-reading the whole conversation. Re-fetch current state rather than relying on earlier-in-conversation values, since PRs may have opened since:

```bash
gh pr list --repo Homebrew/homebrew-core --search "gitversion <VERSION>" --state all --json number,title,state,url --jq '.[]'
gh pr list --repo microsoft/winget-pkgs --search "GitTools.GitVersion version <VERSION>" --state all --json number,title,state,url --jq '.[]'
gh pr list --repo GitTools/actions --search "gitversion <VERSION>" --state all --limit 5 --json number,title,state,url --jq '.[]'
```

**Once a downstream PR is confirmed created, report only its link — its merge state is out of scope.** PR creation marks the end of this skill's tracking for that target; merge timing depends on third-party review and is not tracked or reported.

Present a table covering every PR/issue touched during the run — not just downstream ones. Include rows for: the milestone created/reused, any items relabeled or moved into it, and the three downstream publish PRs (Homebrew, winget, and GitTools Actions all open PRs). Mark anything that never got a PR (e.g. a publish job failed, or GitTools Actions pushed directly to main) so it's visually distinct from "still pending."

```
Release Summary: <VERSION>
─────────────────────────────────────────────────────
Release:     https://github.com/GitTools/GitVersion/releases/tag/<VERSION>
Discussion:  <Announcements discussion url> / ⚠️ none linked
Milestone:   https://github.com/GitTools/GitVersion/milestone/<MILESTONE_NUMBER> (N items)

Downstream PRs:
📦 Homebrew          <url> / not yet created (<reason>)
📦 winget            <url> / not yet created (CI still running)
⚙️  GitTools Actions  <url> / no PR needed (confirmed no-op) / not yet created (CI still running) / ⚠️ pushed directly to main (unexpected): <commit url>

Other issues/PRs touched:
- [#NUMBER](url) — relabeled <old> → <new> / moved into milestone / etc. (only list non-obvious actions, not every routine milestone assignment)
```

Keep this table updated in place (don't re-print a full new one) each time the maintainer asks for status later in the same session — re-run the three `gh pr list` commands above to refresh state rather than assuming nothing changed.

### Local cleanup

Before closing the workflow, remove only temporary files created for release-note
recovery or verification, then run:

```bash
git status --short
```

Report any intentional local change separately (for example, an update to this
skill). Never delete or reset unrelated user work as part of release cleanup.
