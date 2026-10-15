---
Title: Set up GitVersion with an AI agent
Order: 3
Description: Help a coding agent install GitVersion v7 and verify configuration against your repository's history.
AgentOnboarding: true
---
Use this guide with Claude Code, Codex, GitHub Copilot, or Antigravity in **the repository you want to version**. The agent should recommend settings from your release process and check them against existing history before adopting them.

The primary path uses GitVersion **v7's established CLI**, built from `src/` in the GitVersion source repository. The experimental `new-cli/` tree has a different command structure and is outside this guide. If you retain an existing v6 installation, use the [optional v6 path][v6-path].

## Give the agent this task

Use **Copy setup prompt**, or choose your agent from **Use with an agent**, to copy the task with this guide's links. Paste it into the agent session for your repository. You can also [view the Markdown guide][markdown-guide] or select the prompt below if copying is unavailable.

Open your repository in your agent and provide this prompt together with this guide's URL or a local copy:

```text
Read the GitVersion AI-agent onboarding guide. Inspect this repository's
existing GitVersion installation, configuration, branch/tag history and
release workflow. Recommend a pinned v7 distribution and minimal configuration
that match our workflow, explaining alternatives and assumptions. Keep v6 as
an option if we already depend on it or choose its compatibility guarantees.

Show proposed installation commands and file changes before applying them
unless I have already authorized those actions. After installation, verify
the selected executable's version and supported commands, inspect the effective
configuration, then compare baseline and candidate results on representative
existing branches, tags and commits. Explain unexpected versions before
adjusting settings. Report evidence, remaining questions and the final diff.

Preserve existing files, user edits and Git history. Do not create tags or
commits, fetch or normalize refs merely to make a sample work, install globally,
change authentication, publish, push or configure CI unless authorized.
Do not read or expose secrets. Follow this repository's instructions.
```

For reproducible team use, pin the guide to a reviewed GitVersion source commit or vendor a local copy and record its revision. A URL in an instruction file does not guarantee that an agent retrieves it: explicitly ask the agent to read it and report the guide revision and tool version it used.

## 1. Inspect before changing anything

The agent should establish:

* Repository root, current branch and commit, dirty/untracked paths, shallow status, and whether this is a linked worktree.
* Existing executables and their `--version`/`--help` output; local and relevant ancestor tool manifests; GitVersion package references, scripts, and CI tool pins. A PATH executable can differ from the local tool or CI version.
* Configuration in the working directory and repository root: `GitVersion.yml`, `GitVersion.yaml`, `.GitVersion.yml`, `.GitVersion.yaml`, and explicit paths in scripts. Resolve ambiguous files explicitly rather than creating another one.
* OS, architecture, shell, and available .NET SDK/runtime or Docker. Separate the tool's requirements from the language and target frameworks of the application being versioned.
* Existing release tags, branch names and a bounded history graph; merge, squash or rebase practice; release branches, prerelease channels and version consumers.

Ask about release intent only where the evidence leaves a choice unresolved. For example, a branch named `develop` does not establish a Git Flow release policy. For a monorepo, agree whether components share one version before choosing settings. Inspect scoped project and workflow files; avoid `.env`, credential files, authenticated remote URLs and environment dumps.

Record a baseline before editing. Preserve existing configuration and tool pins until the user accepts a replacement. See [repository requirements][requirements] for the history needed for a meaningful calculation.

## 2. Select and acquire v7

Choose a distribution based on available tooling and intended consumers:

| Situation | Suggested option | Tradeoff |
| --- | --- | --- |
| Existing compatible pinned installation | Reuse it | Align local and CI versions before changing either. |
| A .NET SDK is already available | Pinned local `GitVersion.Tool` | A manifest keeps the project and team on one version. v7 tool installation requires the .NET 10 SDK. |
| Docker is already available | Versioned image, preferably pinned by digest | Avoid host .NET setup; account for platform and writable mounts. |
| Neither SDK nor Docker is appropriate | Standalone release archive | Select the actual OS/architecture and Linux glibc/musl asset; keep it in a project/task-local location. |
| .NET assembly/package versions should follow builds | Consider `GitVersion.MsBuild` after CLI verification | This changes build behavior; coordinate existing version writers and supported MSBuild hosts. |
| CI already integrates GitVersion | Match the integration's tool pin | The action/task version and GitVersion version are separate choices. |

GitVersion can calculate versions for JavaScript, Python, Go and other repositories too. Start with JSON output and decide which variable each consumer needs. Refer to [installation][installation], [MSBuild integration][msbuild] and [CI integration][ci] for details. Offer workstation-wide package managers or global tools only when the user wants global setup.

### Pin a real artifact or source revision

Check [published releases][releases] and [NuGet packages][nuget] before installing. These are v7 development instructions: **documentation for v7 does not establish that a stable v7 package or image is published**. Do not let an unversioned install silently select v6. Verify the exact chosen package, archive or image and its provenance; do not invent a tag, asset or checksum file.

For an existing local manifest, inspect its entries before restoring tools:

```shell
dotnet tool restore
dotnet tool run dotnet-gitversion -- --version
dotnet tool run dotnet-gitversion -- --help
```

For a new manifest, create it only when no applicable manifest exists, then install an exact, verified v7 package version. These are templates: replace angle-bracket values before execution.

```text
dotnet new tool-manifest
dotnet tool install GitVersion.Tool --version <verified-v7-package-version>
```

Preserve other tools in an existing manifest. Do not change feeds or credentials to make installation succeed. If using .NET 10's one-shot `dotnet tool exec`, verify its version resolution: without a pin or applicable manifest it can download the latest package.

If a suitable v7 artifact is unavailable, offer a source build in a **separate GitVersion checkout**, at a reviewed v7 commit. Use the SDK pinned in that checkout's `global.json`, publish `src/GitVersion.App/GitVersion.App.csproj`, and record the source SHA and resulting executable path. See the [source-build example][getting-started]. Building requires restoring dependencies; explain that setup cost before proceeding. Do not build experimental `new-cli/` as a substitute.

In the remaining v7 examples, `gitversion` means the executable you verified. For a local tool replace it with `dotnet tool run dotnet-gitversion --`; for a source build use `dotnet /absolute/path/to/gitversion.dll`. Confirm `--version`, `--help` and, when supported, [the CLI schema][cli-schema] before selecting commands. Store both the pin and invocation in the handoff.

### Docker mounts and permissions

Use the selected image's exact version/platform. v7 images run as UID/GID `10001:10001`; the mounted repository must be writable by that user. On Linux, the non-root checkout owner's UID/GID may be appropriate. v7 handles an unwritable home using a private temporary home inside the container, without extra `HOME` or `DOTNET_CLI_HOME` flags. On Docker Desktop, check host file-sharing permissions. Follow [Docker repository permissions][docker-permissions].

Prefer a disposable standalone clone for Docker experiments. A linked worktree's `.git` file can point outside the mount, and GitVersion may write repository metadata/cache, so a read-only mount is not a general solution. Mount a candidate configuration deliberately and pass its container path. Do not use recursive permission changes, root or privileged execution as a workaround. Container environment changes do not universally reach the host; use JSON for these checks.

## 3. Recommend a minimal configuration

Choose defaults that fit how the repository already merges and releases. Present alternatives with evidence and assumptions:

| Observed practice | Candidate starting point |
| --- | --- |
| Short-lived branches merged into main through PRs | `GitHubFlow/v1` |
| Persistent development and release stages | `GitFlow/v1` |
| Trunk-based practice | Evaluate existing presets first; offer `TrunkBased/preview1` only with its experimental status explained. |

A preset is a starting point, not a requirement to reorganize branches. Keep [workflow, deployment mode and calculation strategy][workflow-choices] separate. Discuss version increments, tag recognition, labels and package/assembly output needs independently.

For a repository that selects GitHub Flow, this is a minimal v7 candidate:

```yaml
workflow: GitHubFlow/v1
```

Add overrides only for a stated requirement. For example, v7 calculation overrides belong under `calculation`:

```yaml
workflow: GitHubFlow/v1
calculation:
  tag-prefix: '[vV]?'
```

That tag prefix is already the default; do not add it unless explicitly recording it helps the team. Inspect existing tag names before changing it. Output settings belong under `output`. Use [configuration by topic][configuration-topics] and the [reference][configuration] rather than copying every resolved default into the file.

Save a candidate separately from the existing config, preserving comments and unrelated settings. Select the candidate by absolute path during checks. Do not set `next-version` or ignore history merely to force a desired example result. For a v6-to-v7 migration, emit a separate candidate with [config migrate][migration] and review the diff; in-place migration cannot preserve comments.

## 4. Check structure and resolved settings

Associate the candidate with the matching [configuration schema][v7-schema] in the editor or a YAML-aware validator. The v7 schema describes nested YAML. Editor schema comments/associations do not require adding a runtime `$schema` property. Schema validation checks structure; loading and history calculations check additional behavior.

These templates use paths selected during inspection:

```text
gitversion --target-path <repository> --config <candidate-file> --show-config
gitversion --target-path <repository> config get calculation.tag-prefix --config <candidate-file>
```

`--show-config` displays defaults and overrides. `config get` returns a JSON scalar for a supported v7 property; it does not return branch-specific inherited behavior or a calculated version. Both need a suitable repository in the established CLI. See [configuration queries][config-queries].

**A dedicated `config validate` command is proposed in [issue #5360][config-validate]. It is not an available step in this guide.** Until it ships, combine schema checks, effective-config inspection and representative calculations. If a future selected executable supports the command, check its documented contract before using it. Successful validation will still need history-based verification to establish that versions fit the release workflow.

## 5. Compare results against real history

Agree on representative existing points and expected behavior before tuning the candidate:

| Sample | Question to resolve |
| --- | --- |
| Current HEAD | What version/channel should the next build produce? |
| Latest reachable release tag and its commit | Does GitVersion recognize the released version? |
| Existing commit after that tag | Does the increment match the release policy? |
| Existing feature/PR branch | Are the label, number and consumer version suitable? |
| Existing merge, release or support branch point | Does the version progress as expected across release stages? |

Record branch, full commit SHA, tag if applicable, selected tool/config and baseline/candidate results. If a scenario does not exist, mark it untested. Do not create tags, commits or branches to manufacture a sample in the user's repository.

Use the verified v7 executable's branch and commit selection instead of switching the shared checkout. This template selects actual existing refs/commits:

```text
gitversion --target-path <repository> --config <candidate-file> --output json --no-fetch --no-normalize --no-cache --branch <existing-branch> --commit <existing-sha>
```

Run each sample with the baseline and the candidate. When no baseline file exists, omit `--config` for the baseline and record that defaults were used. Keep the same selected file across samples rather than accidentally loading historical configuration. A commit reachable from multiple branches needs explicit branch context; a detached release tag may not reproduce the release branch's calculation.

Capture exit status, stdout JSON and stderr separately. Keep build-server context out of local experiments and use JSON without assembly/project/WiX update options. `--no-fetch`, `--no-normalize` and `--no-cache` avoid fetching, build-server normalization and cache writes during comparison. For uncertain environments, use an isolated standalone copy with the existing relevant refs/history, preserving branch identity; do not normalize or synthesize refs just to make the calculation work.

Compare `SemVer`, `FullSemVer`, `Sha`, `BranchName`, prerelease fields and `VersionSourceDistance` with the user's expectations. Check [variable meanings][variables] and the actual package/assembly consumer's constraints. A prerelease number is not a count of CI runs.

When results differ, investigate in this order:

1. Selected executable/version, parser, config layout and config path.
2. Available history/tags, branch context and version source.
3. Increment, mode, inheritance and label policy.
4. Output formatting and downstream consumers.

Use `--diagnose` or diagnostic verbosity when needed, keeping logs local and redacting paths, remote details and sensitive history before sharing. Explain one adjustment at a time, rerun affected samples and retain earlier agreed checks. If missing history prevents a conclusion, report the gap and propose an authorized fetch into a disposable clone; allowing a shallow calculation does not prove correctness. Stop for an unresolved release-policy decision instead of tuning indefinitely.

## 6. Adopt and hand off

Within the user's authorization, apply the minimal accepted config change and retain the selected version pin. Return:

* Installation method, exact tool version/source SHA, guide revision and invocation.
* Configuration diff, preset rationale, alternatives and remaining assumptions.
* Baseline/candidate sample table with expected/actual versions, exit status and untested scenarios.
* Reproduction commands and any blockers or diagnostic findings.

Check files and refs against the initial inventory and explain intentional changes. Keep user edits intact. Add MSBuild or CI integration only when requested, after CLI results agree. Avoid duplicate assembly-version writers; preserve the selected integration's output contract, including `GitVersion_` prefixes where applicable. Consult [v7 migration changes][migration] before updating consumers. Release creation, tagging, publishing and pushing require their own authorization.

## Optional v6 compatibility path

Retain v6 when the user chooses it or an existing pin depends on it. Verify that exact published version and its help; do not automatically migrate its config. v6 uses flat settings, for example:

```yaml
workflow: GitHubFlow/v1
tag-prefix: '[vV]?'
```

Use the appropriate flat configuration schema, such as [the 6.3 schema][v6-schema] for compatible v6 versions, rather than the nested v7 schema. Schema versions do not necessarily match tool patch numbers. Keep v6 examples in a separate recipe:

```text
<verified-v6-executable> <repository> /config <candidate-file> /showconfig
<verified-v6-executable> <repository> /config <candidate-file> /output json /nofetch /nonormalize /nocache /b <existing-branch> /c <existing-sha>
```

Use the same baseline/candidate history loop, but read v6 variable names, such as `CommitsSinceVersionSource`, from that tool's reference. `config get`, `config migrate` and `--cli-schema` are v7 features; the proposed validator does not add a command to published v6 binaries. See [the migration guide][migration] before selecting v7, including the independent temporary configuration/parser/backend selectors and their removal schedule. v7's temporary flat-v6 configuration mode is distinct from running a v6 executable.

## Optional agent discovery adapters

The explicit prompt above works without modifying instruction files. GitVersion's own maintainer instructions are not automatically discovered from the user's repository. For repeated use, merge a small task-specific pointer into that repository's existing instructions, preserving their contents and scope:

| Agent | Where to put the pointer | Discovery caveat |
| --- | --- | --- |
| Codex | Existing `AGENTS.md` | Overrides and nearer directory instructions affect discovery; see [Codex guidance][codex-instructions]. |
| Claude Code | Existing `CLAUDE.md`, optionally an `@path` import of a local guide | Current `AGENTS.md` support depends on client version/settings and existing CLAUDE files; see [Claude memory][claude-instructions]. |
| GitHub Copilot | `.github/copilot-instructions.md` | `AGENTS.md` and path-specific support vary by IDE/feature; check [the support matrix][copilot-instructions]. |
| Antigravity | Existing `AGENTS.md`/`GEMINI.md` or a scoped `.agents/rules/*.md` file | Modular rules require valid activation frontmatter; see [Antigravity rules][antigravity-instructions]. |

For example, adapt this pointer with the reviewed guide's location:

```text
When asked to adopt or adjust GitVersion, read our pinned copy of the
GitVersion AI-agent onboarding guide at <reviewed-guide-location>.
Use its inspect/propose/verify flow, preserve existing files and refs,
and report the tool/config versions and history samples checked.
```

These are optional adapters to one canonical guide. Avoid copying the full procedure into four files, changing global agent settings, or assuming a link is an automatic import. Verify discovery in the actual client, then have the agent explicitly read the guide.

[v6-path]: #optional-v6-compatibility-path

[markdown-guide]: ./ai-agent-onboarding.md

[requirements]: /docs/reference/requirements

[installation]: /docs/usage/cli/installation

[msbuild]: /docs/usage/msbuild

[ci]: /docs/reference/build-servers

[releases]: https://github.com/GitTools/GitVersion/releases

[nuget]: https://www.nuget.org/packages/GitVersion.Tool

[getting-started]: /docs/getting-started#2-install-gitversion

[cli-schema]: /docs/usage/cli/cli-schema

[docker-permissions]: /docs/usage/docker#non-root-containers-and-repository-permissions

[workflow-choices]: /docs/usage/choose-workflow

[configuration-topics]: /docs/reference/configuration-topics

[configuration]: /docs/reference/configuration

[migration]: /docs/migration/v6-to-v7

[v7-schema]: https://gitversion.net/schemas/7.0/GitVersion.configuration.json

[config-queries]: /docs/usage/cli/arguments#query-a-configuration-property

[config-validate]: https://github.com/GitTools/GitVersion/issues/5360

[variables]: /docs/reference/variables

[v6-schema]: https://gitversion.net/schemas/6.3/GitVersion.configuration.json

[codex-instructions]: https://developers.openai.com/codex/guides/agents-md

[claude-instructions]: https://code.claude.com/docs/en/memory

[copilot-instructions]: https://docs.github.com/en/copilot/reference/custom-instructions-support

[antigravity-instructions]: https://antigravity.google/docs/rules/
