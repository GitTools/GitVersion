---
Order: 10
Title: Configuration
Description: Details about how GitVersion can be configured to suit your needs
RedirectFrom: docs/configuration
---

GitVersion, starting from version 3.0, is mainly powered by configuration and no
longer has branching strategies hard-coded.

:::{.alert .alert-info}
**Note**

GitVersion ships with internal default configuration which works with
GitHubFlow and GitFlow, probably with others too.
:::

The `develop` branch is set to `ContinuousDeployment` mode by default as we have
found that is generally what is needed when using GitFlow.

To see the effective configuration (defaults and overrides), you can run
`gitversion /showConfig`.

## Global configuration

The following supported workflow configurations are available in GitVersion and can be referenced by the workflow property:

* GitFlow (GitFlow/v1)
* GitHubFlow (GitHubFlow/v1)
* TrunkBased (TrunkBased/preview1)

Example of using a `GitHubFlow` workflow with a different `tag-prefix`:

```yaml
workflow: GitHubFlow/v1
tag-prefix: '[abc]'
```

The built-in configuration for the `GitFlow` workflow (`workflow: GitFlow/v1`) looks like:

<!-- snippet: /docs/workflows/GitFlow/v1.yml -->
<a id='snippet-/docs/workflows/GitFlow/v1.yml'></a>
```yml
assembly-versioning-scheme: MajorMinorPatch
assembly-file-versioning-scheme: MajorMinorPatch
tag-prefix: '[vV]?'
version-in-branch-pattern: (?<version>[vV]?\d+(\.\d+)?(\.\d+)?).*
major-version-bump-message: \+semver:\s?(breaking|major)
minor-version-bump-message: \+semver:\s?(feature|minor)
patch-version-bump-message: \+semver:\s?(fix|patch)
no-bump-message: \+semver:\s?(none|skip)
tag-pre-release-weight: 60000
commit-date-format: yyyy-MM-dd
merge-message-formats: {}
update-build-number: true
semantic-version-format: Strict
strategies:
- Fallback
- ConfiguredNextVersion
- MergeMessage
- TaggedCommit
- TrackReleaseBranches
- VersionInBranchName
branches:
  develop:
    mode: ContinuousDelivery
    label: alpha
    increment: Minor
    prevent-increment:
      when-current-commit-tagged: false
    track-merge-target: true
    track-merge-message: true
    regex: ^dev(elop)?(ment)?$
    source-branches:
    - main
    is-source-branch-for: []
    tracks-release-branches: true
    is-release-branch: false
    is-main-branch: false
    pre-release-weight: 0
  main:
    label: ''
    increment: Patch
    prevent-increment:
      of-merged-branch: true
    track-merge-target: false
    track-merge-message: true
    regex: ^master$|^main$
    source-branches: []
    is-source-branch-for: []
    tracks-release-branches: false
    is-release-branch: false
    is-main-branch: true
    pre-release-weight: 55000
  release:
    mode: ManualDeployment
    label: beta
    increment: Minor
    prevent-increment:
      of-merged-branch: true
      when-current-commit-tagged: false
    track-merge-target: false
    regex: ^releases?[\/-](?<BranchName>.+)
    source-branches:
    - main
    - support
    is-source-branch-for: []
    tracks-release-branches: false
    is-release-branch: true
    is-main-branch: false
    pre-release-weight: 30000
  feature:
    mode: ManualDeployment
    label: '{BranchName}'
    increment: Inherit
    prevent-increment:
      when-current-commit-tagged: false
    track-merge-message: true
    regex: ^features?[\/-](?<BranchName>.+)
    source-branches:
    - develop
    - main
    - release
    - support
    - hotfix
    is-source-branch-for: []
    is-main-branch: false
    pre-release-weight: 30000
  pull-request:
    mode: ContinuousDelivery
    label: PullRequest{Number}
    increment: Inherit
    prevent-increment:
      of-merged-branch: true
      when-current-commit-tagged: false
    track-merge-message: true
    regex: ^(pull-requests|pull|pr)[\/-](?<Number>\d*)
    source-branches:
    - develop
    - main
    - release
    - feature
    - support
    - hotfix
    is-source-branch-for: []
    pre-release-weight: 30000
  hotfix:
    mode: ManualDeployment
    label: beta
    increment: Inherit
    prevent-increment:
      when-current-commit-tagged: false
    regex: ^hotfix(es)?[\/-](?<BranchName>.+)
    source-branches:
    - main
    - support
    is-source-branch-for: []
    is-release-branch: true
    is-main-branch: false
    pre-release-weight: 30000
  support:
    label: ''
    increment: Patch
    prevent-increment:
      of-merged-branch: true
    track-merge-target: false
    regex: ^support[\/-](?<BranchName>.+)
    source-branches:
    - main
    is-source-branch-for: []
    tracks-release-branches: false
    is-release-branch: false
    is-main-branch: true
    pre-release-weight: 55000
  unknown:
    mode: ManualDeployment
    label: '{BranchName}'
    increment: Inherit
    prevent-increment:
      when-current-commit-tagged: true
    regex: (?<BranchName>.+)
    source-branches:
    - main
    - develop
    - release
    - feature
    - pull-request
    - hotfix
    - support
    is-source-branch-for: []
    is-main-branch: false
ignore:
  sha: []
  paths: []
mode: ContinuousDelivery
label: '{BranchName}'
increment: Inherit
prevent-increment:
  of-merged-branch: false
  when-branch-merged: false
  when-current-commit-tagged: true
track-merge-target: false
track-merge-message: true
commit-message-incrementing: Enabled
regex: ''
source-branches: []
is-source-branch-for: []
tracks-release-branches: false
is-release-branch: false
is-main-branch: false
```
<sup><a href='/docs/workflows/GitFlow/v1.yml#L1-L167' title='Snippet source file'>snippet source</a> | <a href='#snippet-/docs/workflows/GitFlow/v1.yml' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The supported built-in configuration for the `GitHubFlow` workflow (`workflow: GitHubFlow/v1`) looks like:

<!-- snippet: /docs/workflows/GitHubFlow/v1.yml -->
<a id='snippet-/docs/workflows/GitHubFlow/v1.yml'></a>
```yml
assembly-versioning-scheme: MajorMinorPatch
assembly-file-versioning-scheme: MajorMinorPatch
tag-prefix: '[vV]?'
version-in-branch-pattern: (?<version>[vV]?\d+(\.\d+)?(\.\d+)?).*
major-version-bump-message: \+semver:\s?(breaking|major)
minor-version-bump-message: \+semver:\s?(feature|minor)
patch-version-bump-message: \+semver:\s?(fix|patch)
no-bump-message: \+semver:\s?(none|skip)
tag-pre-release-weight: 60000
commit-date-format: yyyy-MM-dd
merge-message-formats: {}
update-build-number: true
semantic-version-format: Strict
strategies:
- Fallback
- ConfiguredNextVersion
- MergeMessage
- TaggedCommit
- TrackReleaseBranches
- VersionInBranchName
branches:
  main:
    label: ''
    increment: Patch
    prevent-increment:
      of-merged-branch: true
    track-merge-target: false
    track-merge-message: true
    regex: ^master$|^main$
    source-branches: []
    is-source-branch-for: []
    tracks-release-branches: false
    is-release-branch: false
    is-main-branch: true
    pre-release-weight: 55000
  release:
    mode: ManualDeployment
    label: beta
    increment: Patch
    prevent-increment:
      of-merged-branch: true
      when-branch-merged: false
      when-current-commit-tagged: false
    track-merge-target: false
    track-merge-message: true
    regex: ^releases?[\/-](?<BranchName>.+)
    source-branches:
    - main
    is-source-branch-for: []
    tracks-release-branches: false
    is-release-branch: true
    is-main-branch: false
    pre-release-weight: 30000
  feature:
    mode: ManualDeployment
    label: '{BranchName}'
    increment: Inherit
    prevent-increment:
      when-current-commit-tagged: false
    track-merge-message: true
    regex: ^features?[\/-](?<BranchName>.+)
    source-branches:
    - main
    - release
    is-source-branch-for: []
    is-main-branch: false
    pre-release-weight: 30000
  pull-request:
    mode: ContinuousDelivery
    label: PullRequest{Number}
    increment: Inherit
    prevent-increment:
      of-merged-branch: true
      when-current-commit-tagged: false
    track-merge-message: true
    regex: ^(pull-requests|pull|pr)[\/-](?<Number>\d*)
    source-branches:
    - main
    - release
    - feature
    is-source-branch-for: []
    pre-release-weight: 30000
  unknown:
    mode: ManualDeployment
    label: '{BranchName}'
    increment: Inherit
    prevent-increment:
      when-current-commit-tagged: false
    track-merge-message: false
    regex: (?<BranchName>.+)
    source-branches:
    - main
    - release
    - feature
    - pull-request
    is-source-branch-for: []
    is-main-branch: false
ignore:
  sha: []
  paths: []
mode: ContinuousDelivery
label: '{BranchName}'
increment: Inherit
prevent-increment:
  of-merged-branch: false
  when-branch-merged: false
  when-current-commit-tagged: true
track-merge-target: false
track-merge-message: true
commit-message-incrementing: Enabled
regex: ''
source-branches: []
is-source-branch-for: []
tracks-release-branches: false
is-release-branch: false
is-main-branch: false
```
<sup><a href='/docs/workflows/GitHubFlow/v1.yml#L1-L116' title='Snippet source file'>snippet source</a> | <a href='#snippet-/docs/workflows/GitHubFlow/v1.yml' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The preview built-in configuration (experimental usage only) for the `TrunkBased` workflow (`workflow: TrunkBased/preview1`) looks like:

<!-- snippet: /docs/workflows/TrunkBased/preview1.yml -->
<a id='snippet-/docs/workflows/TrunkBased/preview1.yml'></a>
```yml
assembly-versioning-scheme: MajorMinorPatch
assembly-file-versioning-scheme: MajorMinorPatch
tag-prefix: '[vV]?'
version-in-branch-pattern: (?<version>[vV]?\d+(\.\d+)?(\.\d+)?).*
major-version-bump-message: \+semver:\s?(breaking|major)
minor-version-bump-message: \+semver:\s?(feature|minor)
patch-version-bump-message: \+semver:\s?(fix|patch)
no-bump-message: \+semver:\s?(none|skip)
tag-pre-release-weight: 60000
commit-date-format: yyyy-MM-dd
merge-message-formats: {}
update-build-number: true
semantic-version-format: Strict
strategies:
- ConfiguredNextVersion
- Mainline
branches:
  main:
    mode: ContinuousDeployment
    label: ''
    increment: Patch
    prevent-increment:
      of-merged-branch: true
    track-merge-target: false
    track-merge-message: true
    regex: ^master$|^main$
    source-branches: []
    is-source-branch-for: []
    tracks-release-branches: false
    is-release-branch: false
    is-main-branch: true
    pre-release-weight: 55000
  feature:
    mode: ContinuousDelivery
    label: '{BranchName}'
    increment: Minor
    prevent-increment:
      when-current-commit-tagged: false
    track-merge-message: true
    regex: ^features?[\/-](?<BranchName>.+)
    source-branches:
    - main
    is-source-branch-for: []
    is-main-branch: false
    pre-release-weight: 30000
  hotfix:
    mode: ContinuousDelivery
    label: '{BranchName}'
    increment: Patch
    prevent-increment:
      when-current-commit-tagged: false
    regex: ^hotfix(es)?[\/-](?<BranchName>.+)
    source-branches:
    - main
    is-source-branch-for: []
    is-release-branch: true
    is-main-branch: false
    pre-release-weight: 30000
  pull-request:
    mode: ContinuousDelivery
    label: PullRequest{Number}
    increment: Inherit
    prevent-increment:
      of-merged-branch: true
      when-current-commit-tagged: false
    track-merge-message: true
    regex: ^(pull-requests|pull|pr)[\/-](?<Number>\d*)
    source-branches:
    - main
    - feature
    - hotfix
    is-source-branch-for: []
    pre-release-weight: 30000
  unknown:
    increment: Patch
    prevent-increment:
      when-current-commit-tagged: false
    regex: (?<BranchName>.+)
    source-branches:
    - main
    is-source-branch-for: []
    pre-release-weight: 30000
ignore:
  sha: []
  paths: []
mode: ContinuousDelivery
label: '{BranchName}'
increment: Inherit
prevent-increment:
  of-merged-branch: false
  when-branch-merged: false
  when-current-commit-tagged: true
track-merge-target: false
track-merge-message: true
commit-message-incrementing: Enabled
regex: ''
source-branches: []
is-source-branch-for: []
tracks-release-branches: false
is-release-branch: false
is-main-branch: false
```
<sup><a href='/docs/workflows/TrunkBased/preview1.yml#L1-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-/docs/workflows/TrunkBased/preview1.yml' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The details of the available options are as follows:

### workflow

The base template of the configuration to use. Possible values are `GitFlow/v1` or `GitHubFlow/v1`. Defaults to `GitFlow/v1` if not set. To create a configuration from scratch without using a base template, please specify an empty string.

### next-version

Allows you to bump the next version explicitly. Useful for bumping `main` or a
feature branch with breaking changes (i.e., a major increment), indicating what
the next `git tag` is going to be.

`next-version` is not a permanent replacement for `git tag` and should only be
used intermittently. Since version 5.5 GitVersion supports `next-version` with
`mode: Mainline` and should not be treated as a "base version".

If you are using `next-version` and are experiencing weird versioning behaviour,
please remove it, create a `git tag` with an appropriate version number on an
appropriate historical commit and see if that resolves any versioning issues
you may have.

### assembly-versioning-scheme

When updating assembly info, `assembly-versioning-scheme` tells GitVersion how
to treat the `AssemblyVersion` attribute. Useful to lock the major when using
Strong Naming. Note: you can use `None` to skip updating the `AssemblyVersion`
while still updating the `AssemblyFileVersion` and `AssemblyInformationVersion`
attributes. Valid values: `MajorMinorPatchTag`, `MajorMinorPatch`, `MajorMinor`,
`Major`, `None`.

For information on using format strings in these properties, see
[Format Strings](/docs/reference/custom-formatting).

### assembly-file-versioning-scheme

When updating assembly info, `assembly-file-versioning-scheme` tells GitVersion
how to treat the `AssemblyFileVersion` attribute. Note: you can use `None` to
skip updating the `AssemblyFileVersion` while still updating the
`AssemblyVersion` and `AssemblyInformationVersion` attributes. Valid values:
`MajorMinorPatchTag`, `MajorMinorPatch`, `MajorMinor`, `Major`, `None`.

### assembly-file-versioning-format

Specifies the format of `AssemblyFileVersion` and
overwrites the value of `assembly-file-versioning-scheme`.

Expressions in curly braces reference one of the [variables][variables]
or a process-scoped environment variable (when prefixed with `env:`).  For example,

```yaml
# use a variable if non-null or a fallback value otherwise
assembly-file-versioning-format: '{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber ?? 0}'

# use an environment variable or raise an error if not available
assembly-file-versioning-format: '{Major}.{Minor}.{Patch}.{env:BUILD_NUMBER}'

# use an environment variable if available or a fallback value otherwise
assembly-file-versioning-format: '{Major}.{Minor}.{Patch}.{env:BUILD_NUMBER ?? 42}'
```

### assembly-versioning-format

Specifies the format of `AssemblyVersion` and
overwrites the value of `assembly-versioning-scheme`.
Follows the same formatting semantics as `assembly-file-versioning-format`.

### assembly-informational-format

Specifies the format of `AssemblyInformationalVersion`.
Follows the same formatting semantics as `assembly-file-versioning-format`.
The default value is `{InformationalVersion}`.

### mode

Sets the `mode` of how GitVersion should create a new version. Read more at
[deployment modes][modes].

### increment

The part of the SemVer to increment when GitVersion detects it needs to be
increased, such as for commits after a tag: `Major`, `Minor`, `Patch`, `None`.

The special value `Inherit` means that GitVersion should find the parent branch
(i.e. the branch where the current branch was branched from), and use its values
for [increment](#increment) or other branch related properties.

### tag-prefix

A regular expression which is used to trim Git tags before processing (e.g.,
v1.0.0). The default value is `[vV]`.

### version-in-branch-pattern

A regular expression which is used to determine the version number in the branch
name or commit message (e.g., v1.0.0-LTS). This setting only applies on branches
where the option `is-release-branch` is set to `true`. The default value is
`(?<version>[vV]?\d+(\.\d+)?(\.\d+)?).*`.

### major-version-bump-message

The regex to match commit messages with to perform a major version increment.
Default set to `'\+semver:\s?(breaking|major)'`, which will match occurrences of
`+semver: major` and `+semver: breaking` in a commit message.

### minor-version-bump-message

The regex to match commit messages with to perform a minor version increment.
Default set to `'\+semver:\s?(feature|minor)'`, which will match occurrences of
`+semver: feature` and `+semver: minor` in a commit message.

### patch-version-bump-message

The regex to match commit messages with to perform a patch version increment.
Default set to `'\+semver:\s?(fix|patch)'`, which will match occurrences of
`+semver: fix` and `+semver: patch` in a commit message.

### no-bump-message

Used to tell GitVersion not to increment when in Mainline development mode.
Default `\+semver:\s?(none|skip)`, which will match occurrences of `+semver:
none` and `+semver: skip`

When a commit matches **both** the `no-bump-message` **and** any combination of
the `version-bump-message`, `no-bump-message` takes precedence and no increment is applied.

### tag-pre-release-weight

The pre-release weight in case of tagged commits. If the value is not set in the
configuration, a default weight of 60000 is used instead. If the
`WeightedPreReleaseNumber` [variable][variables] is 0 and this parameter is set,
its value is used. This helps if your branching model is GitFlow and the last
release build, which is often tagged, can utilize this parameter to produce a
monotonically increasing build number.

### commit-message-incrementing

Sets whether it should be possible to increment the version with special syntax
in the commit message. See the `*-version-bump-message` options above for
details on the syntax. Default set to `Enabled`; set to `Disabled` to disable.

### commit-date-format

Sets the format which will be used to format the `CommitDate` output variable.

### custom-version-format

Specifies the format of `CustomVersion`, allowing for a user-specific output variable
that can be used, for example, as the NuGet package vesion.
Default set to `{SemVer}`. 
Follows the same formatting semantics as `assembly-file-versioning-format`.

### ignore

The header property for the `ignore` configuration.

:::{.alert .alert-info}
**Note:** When ignoring a commit or a range of commits, they are only ignored in
the search for a [version source][version-sources], not when calculating other
parts of the version number, such as build metadata.
:::

#### sha

A sequence of SHAs to be excluded from the version calculations. Useful when
there is a rogue commit in history yielding a bad version. You can use either
style below:

```yaml
ignore:
  sha: [e7bc24c0f34728a25c9187b8d0b041d935763e3a, 764e16321318f2fdb9cdeaa56d1156a1cba307d7]
```

or

```yaml
ignore:
  sha:
    - e7bc24c0f34728a25c9187b8d0b041d935763e3a
    - 764e16321318f2fdb9cdeaa56d1156a1cba307d7
```

#### commits-before

Date and time in the format `yyyy-MM-ddTHH:mm:ss` (eg `commits-before:
2015-10-23T12:23:15`) to setup an exclusion range. Effectively any commit before
`commits-before` will be ignored.

#### paths
A sequence of regular expressions that represent paths in the repository. Commits that modify these paths will be excluded from version calculations. For example, to filter out commits that belong to `docs`:
```yaml
ignore:
  paths:
    - ^docs\/
```
##### *Monorepo*
This ignore config can be used to filter only those commits that belong to a specific project in a monorepo.
As an example, consider a monorepo consisting of subdirectories for `ProjectA`, `ProjectB` and a shared `LibraryC`. For GitVersion to consider only commits that are part of `projectA` and shared library `LibraryC`, a regex that matches all paths except those starting with `ProjectA` or `LibraryC` can be used. Either one of the following configs would filter out `ProjectB`.
* Specific match on `/ProjectB/*`:
```yaml
ignore:
  paths:
    - `^\/ProductB\/.*`
```
* Negative lookahead on anything other than `/ProjectA/*` and `/LibraryC/*`:
```yaml
ignore:
  paths:
    - `^(?!\/ProductA\/|\/LibraryC\/).*`
```
A commit having changes only in `/ProjectB/*` path would be ignored. A commit having changes in the following paths wouldn't be ignored:
* `/ProductA/*`
* `/LibraryC/*`
* `/ProductA/*` and  `/LibraryC/*`
* `/ProductA/*` and `/ProductB/*`
* `/LibraryC/*` and `/ProductB/*`
* `/ProductA/*` and `/ProductB/*` and `/LibraryC/*`

:::
Note: The `ignore.paths` configuration is case-sensitive. This can lead to unexpected behavior on case-insensitive file systems, such as Windows. To ensure consistent matching regardless of case, you can prefix your regular expressions with the case-insensitive flag `(?i)`. For example, `(?i)^docs\/` will match both `docs/` and `Docs/`.
:::

::: {.alert .alert-warning}
A commit is ignored by the `ignore.paths` configuration only if **all paths** changed in that commit match one or more of the specified regular expressions. If a path in a commit does not match any one of the ignore patterns, that commit will be included in version calculations.
:::

### merge-message-formats

Custom merge message formats to enable identification of merge messages that do not
follow the built-in conventions.  Entries should be added as key-value pairs where
the value is a regular expression.
e.g.

```yaml
merge-message-formats:
    tfs: '^Merged (?:PR (?<PullRequestNumber>\d+)): Merge (?<SourceBranch>.+) to (?<TargetBranch>.+)'
```

The regular expression should contain the following capture groups:

* `SourceBranch` - Identifies the source branch of the merge
* `TargetBranch` - Identifies the target branch of the merge
* `PullRequestNumber` - Captures the pull-request number

Custom merge message formats are evaluated _before_ any built in formats.
Support for [Conventional Commits][conventional-commits] can be
[configured][conventional-commits-config].

### update-build-number

Configures GitVersion to update the build number or not when running on a build server.

## Branch configuration

Then we have branch specific configuration, which looks something like this:

:::{.alert .alert-info}
**Note**

v4 changed from using regexes for keys, to named configs
:::

If you have branch specific configuration upgrading to v4 will force you to
upgrade.

```yaml
workflow: 'GitHubFlow/v1'
branches:
  main:
    label: ''
    increment: Patch
    prevent-increment:
      of-merged-branch: true
    track-merge-target: false
    track-merge-message: true
    regex: ^master$|^main$
    source-branches: []
    is-source-branch-for: []
    tracks-release-branches: false
    is-release-branch: false
    is-main-branch: true
    pre-release-weight: 55000
  release:
    mode: ManualDeployment
    label: beta
    increment: Patch
    prevent-increment:
      of-merged-branch: true
      when-branch-merged: false
      when-current-commit-tagged: false
    track-merge-target: false
    track-merge-message: true
    regex: ^releases?[\/-](?<BranchName>.+)
    source-branches:
    - main
    is-source-branch-for: []
    tracks-release-branches: false
    is-release-branch: true
    is-main-branch: false
    pre-release-weight: 30000
  feature:
    mode: ManualDeployment
    label: '{BranchName}'
    increment: Inherit
    prevent-increment:
      when-current-commit-tagged: false
    track-merge-message: true
    regex: ^features?[\/-](?<BranchName>.+)
    source-branches:
    - main
    - release
    is-source-branch-for: []
    is-main-branch: false
    pre-release-weight: 30000
  pull-request:
    mode: ContinuousDelivery
    label: PullRequest{Number}
    increment: Inherit
    prevent-increment:
      of-merged-branch: true
      when-current-commit-tagged: false
    track-merge-message: true
    regex: ^(pull-requests|pull|pr)[\/-](?<Number>\d*)
    source-branches:
    - main
    - release
    - feature
    is-source-branch-for: []
    pre-release-weight: 30000
  unknown:
    mode: ManualDeployment
    label: '{BranchName}'
    increment: Inherit
    prevent-increment:
      when-current-commit-tagged: false
    track-merge-message: false
    regex: (?<BranchName>.+)
    source-branches:
    - main
    - release
    - feature
    - pull-request
    is-source-branch-for: []
    is-main-branch: false
```

If you don't specify the regex, the built-in for that branch config will be
used (recommended).

We don't envision many people needing to change most of these configuration
values, but here they are if you need to:

### regex

This is the regex which is used to match the current branch to the correct
branch configuration.

[Named groups](https://learn.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#named-matched-subexpressions) can be used to dynamically label pre-releases based on the branch name, or parts of it. See [Label](#label) for more details and examples.

### source-branches

Because Git commits only refer to parent commits (not branches) GitVersion
sometimes cannot tell which branch the current branch was branched from.

Take this commit graph

```shell
* release/v1.0.0   * feature/foo
| ________________/
|/
*
*
* (main)
```

By looking at this graph, you cannot tell which of these scenarios happened:

* feature/foo branches off release/v1.0.0
  * Branch release/v1.0.0 from main
  * Branch feature/foo from release/v1.0.0
  * Add a commit to both release/v1.0.0 and feature/foo
  * release/v1.0.0 is the base for feature/foo
* release/v1.0.0 branches off feature/foo
  * Branch feature/foo from main
  * Branch release/v1.0.0 from feature/foo
  * Add a commit to both release/v1.0.0 and feature/foo
  * feature/foo is the base for release/v1.0.0

Or put more simply, you cannot tell which branch was created first,
`release/v1.0.0` or `feature/foo`.

To resolve this issue, we give GitVersion a hint about our branching workflows
by telling it what types of branches a branch can be created from. For example,
feature branches are, by default, configured to have the following source
branches:

`source-branches: ['main', 'develop', 'feature', 'hotfix', 'support']`

This means that we will never bother to evaluate pull request branches as merge
base options and being explicit in this way also improves the performance of
GitVersion.

### is-source-branch-for

The reverse of `source-branches`. This property was introduced to keep it easy
to extend GitVersion's config.

It exists to make it easier to extend GitVersion's configuration. If only
`source-branches` exists and you add a new branch type, for instance
`unstable/`, you then need to re-define the `source-branches` configuration
value for existing branches (like feature/) to now include the new unstable
branch.

A complete example:

```yaml
branches:
  unstable:
    regex: ...
    is-source-branch-for: ['main', 'develop', 'feature', 'hotfix', 'support']
```

Without this configuration value you would have to do:

```yaml
branches:
  unstable:
    regex:
  feature:
    source-branches: ['unstable', 'develop', 'feature', 'hotfix', 'support']
  release:
    source-branches: ['unstable', 'develop']
  etc...
```

### branches

The header for all the individual branch configuration.

### mode

Same as for the [global configuration, explained above](#mode).

### label

The pre-release label to use for this branch. Use the value `{BranchName}` as a placeholder to
insert the value of the named group `BranchName` from the [regular expression](#regex).

For example: branch `feature/foo` would become a pre-release label
of `alpha.foo` with `label: 'alpha.{BranchName}'` and `regex: '^features?[\/-](?<BranchName>.+)'`.

Another example: branch `features/sc-12345/some-description` would become a pre-release label of `sc-12345` with `label: '{StoryNo}'` and `regex: '^features?[\/-](?<StoryNo>sc-\d+)[-/].+'`.

**Note:** To clear a default use an empty string: `label: ''`

### increment

Same as for the [global configuration, explained above](#increment).

### prevent-increment-of-merged-branch

The increment of the branch merged to will be ignored, regardless of whether the merged branch has a version number or not, when this branch related property is set to true on the target branch.

When `release-2.0.0` is merged into main, we want main to build `2.0.0`. If
`release-2.0.0` is merged into develop we want it to build `2.1.0`, this option
prevents incrementing after a versioned branch is merged.

In a GitFlow-based repository, setting this option can have implications on the
`CommitsSinceVersionSource` output variable. It can rule out a potentially
better version source proposed by the `MergeMessageBaseVersionStrategy`. For
more details and an in-depth analysis, please see [the discussion][2506].

### prevent-increment-when-branch-merged

The increment of the merged branch will be ignored when this branch related property is set to `true` on the source branch.

### prevent-increment-when-current-commit-tagged

This branch related property controls the behvior whether to use the tagged (value set to true) or the incremented (value set to false) semantic version. Defaults to true.

### label-number-pattern

Pull requests require us to extract the pre-release number out of the branch
name so `refs/pull/534/merge` builds as `PullRequest534`. This is a regex with
a named capture group called `Number`.

**Example usage:**

```yaml
branches:
  pull-request:
    mode: ContinuousDelivery
    label: PullRequest{Number}
    increment: Inherit
    prevent-increment:
      of-merged-branch: true
      when-current-commit-tagged: false
    track-merge-message: true
    regex: ^(pull-requests|pull|pr)[\/-](?<Number>\d*)
    source-branches:
    - main
    - release
    - feature
    is-source-branch-for: []
    pre-release-weight: 30000
```

### track-merge-target

Strategy which will look for tagged merge commits directly off the current
branch. For example `develop` → `release/1.0.0` → merge into `main` and tag
`1.0.0`. The tag is _not_ on develop, but develop should be version `1.0.0` now.

### track-merge-message

This property is a branch related property and gives the user the possibility to control the behavior of whether the merge
commit message will be interpreted as a next version or not. Consider we have a main branch and a `release/1.0.0` branch and
merge changes from `release/1.0.0` to the `main` branch. If `track-merge-message` is set to `true` then the next version will
be `1.0.0` otherwise `0.0.1`.

### tracks-release-branches

Indicates this branch config represents develop in GitFlow.

### is-release-branch

Indicates this branch config represents a release branch in GitFlow.

### is-main-branch

This indicates that this branch is a main branch. By default `main` and `support/*` are main branches.

### pre-release-weight

Provides a way to translate the `PreReleaseLabel` ([variables][variables]) to a numeric
value in order to avoid version collisions across different branches. For
example, a release branch created after "1.2.3-alpha.55" results in
"1.2.3-beta.1" and thus e.g. "1.2.3-alpha.4" and "1.2.3-beta.4" would have the
same file version: "1.2.3.4". One of the ways to use this value is to set
`assembly-file-versioning-format:
{Major}.{Minor}.{Patch}.{WeightedPreReleaseNumber}`. If the `pre-release-weight`
is set, it would be added to the `PreReleaseNumber` to get a final
`AssemblySemFileVer`, otherwise a branch specific default for
`pre-release-weight` will be used in the calculation. Related Issues [1145][1145]
and [1366][1366].

### semantic-version-format

Specifies the semantic version format that is used when parsing the string.
Can be `Strict` - using the [regex](https://regex101.com/r/Ly7O1x/3/)
or `Loose` the old way of parsing. The default if not specified is `Strict`
Example of invalid `Strict`, but valid `Loose`

```log
1.2-alpha4
01.02.03-rc03
1.2.3.4
```

### strategies

Specifies which version strategy implementation (one or more) will be used to determine the next version.
These strategies can be combined, and the order in which they are specified does not matter.
The configuration accepts the following values:

* Fallback
* ConfiguredNextVersion
* MergeMessage
* TaggedCommit
* TrackReleaseBranches
* VersionInBranchName
* Mainline

[1145]: https://github.com/GitTools/GitVersion/issues/1145

[1366]: https://github.com/GitTools/GitVersion/issues/1366

[2506]: https://github.com/GitTools/GitVersion/pull/2506#issuecomment-754754037

[conventional-commits-config]: /docs/reference/version-increments#conventional-commit-messages

[conventional-commits]: https://www.conventionalcommits.org/

[modes]: /docs/reference/modes

[variables]: /docs/reference/variables

[version-sources]: /docs/reference/version-sources
