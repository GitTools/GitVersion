---
Order: 10
Title: Configuration
Description: Details about how GitVersion can be configured to suit your needs
RedirectFrom: docs/configuration
---

GitVersion, starting from version 3.0, is mainly powered by configuration and no
longer has branching strategies hard-coded.

## Configuration tool

If you run `gitversion init`, GitVersion will launch into a configuration tool,
which can help you configure GitVersion the way you want it.

Once complete, the `init` command will create a `GitVersion.yml` file in the
working directory. It can be the root repository directory or any subdirectory
in case you have a single repository for more than one project or are restricted
to commit into a subdirectory.

:::{.alert .alert-info}
**Note**

GitVersion ships with internal default configuration which works with
GitHubFlow and GitFlow, probably with others too.
:::

The `develop` branch is set to `ContinuousDeployment` mode by default as we have
found that is generally what is needed when using GitFlow.

To see the effective configuration (defaults and overrides), you can run
`gitversion /showConfig`.

To create your config file just type `gitversion init` in your repo directory,
after [installing][installing]. A minimal `GitVersion.yml` configuration file will be
created. Modify this to suit your needs.

## Global configuration

The global configuration looks like this:

```yaml
next-version: 1.0
assembly-versioning-scheme: MajorMinorPatch
assembly-file-versioning-scheme: MajorMinorPatch
assembly-informational-format: '{InformationalVersion}'
mode: ContinuousDelivery
increment: Inherit
continuous-delivery-fallback-tag: ci
tag-prefix: '[vV]'
major-version-bump-message: '\+semver:\s?(breaking|major)'
minor-version-bump-message: '\+semver:\s?(feature|minor)'
patch-version-bump-message: '\+semver:\s?(fix|patch)'
no-bump-message: '\+semver:\s?(none|skip)'
tag-pre-release-weight: 60000
commit-message-incrementing: Enabled
ignore:
  sha: []
  commits-before: yyyy-MM-ddTHH:mm:ss
merge-message-formats: {}
update-build-number: true
```

The details of the available options are as follows:

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
[versioning modes][modes].

### increment

The part of the SemVer to increment when GitVersion detects it needs to be
increased, such as for commits after a tag: `Major`, `Minor`, `Patch`, `None`.

The special value `Inherit` means that GitVersion should find the parent branch
(i.e. the branch where the current branch was branched from), and use its values
for [increment](#increment),
[prevent-increment-of-merged-branch-version](#prevent-increment-of-merged-branch-version)
and [tracks-release-branches](#tracks-release-branches).

### continuous-delivery-fallback-tag

When using `mode: ContinuousDeployment`, the value specified in
`continuous-delivery-fallback-tag` will be used as the pre-release tag for
branches which do not have one specified. Default set to `ci`.

Just to clarify: For a build name without `...-ci-<buildnumber>` or in other
words without a `PreReleaseTag` (ergo `"PreReleaseTag":""` in GitVersion's JSON output)
at the end you would need to set `continuous-delivery-fallback-tag` to an empty
string (`''`):

```yaml
mode: ContinuousDeployment
continuous-delivery-fallback-tag: ''
...
```

Doing so can be helpful if you use your `main` branch as a `release` branch.

### tag-prefix

A regex which is used to trim Git tags before processing (e.g., v1.0.0). Default
is `[vV]`, although this is just for illustrative purposes as we do a IgnoreCase
match and could be `v`.

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
release build, which is often tagged, can utilise this parameter to produce a
monotonically increasing build number.

### commit-message-incrementing

Sets whether it should be possible to increment the version with special syntax
in the commit message. See the `*-version-bump-message` options above for
details on the syntax. Default set to `Enabled`; set to `Disabled` to disable.

### commit-date-format

Sets the format which will be used to format the `CommitDate` output variable.

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

### merge-message-formats

Custom merge message formats to enable identification of merge messages that do not
follow the built-in conventions.  Entries should be added as key-value pairs where
the value is a regular expression.
e.g.

```yaml
merge-message-formats:
    tfs: ^Merged (?:PR (?<PullRequestNumber>\d+)): Merge (?<SourceBranch>.+) to (?<TargetBranch>.+)
```

The regular expression should contain the following capture groups:

*   `SourceBranch` - Identifies the source branch of the merge
*   `TargetBranch` - Identifies the target branch of the merge
*   `PullRequestNumber` - Captures the pull-request number

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
branches:
  main:
    regex: ^master$|^main$
    mode: ContinuousDelivery
    tag: ''
    increment: Patch
    prevent-increment-of-merged-branch-version: true
    track-merge-target: false
    source-branches: [ 'develop', 'release' ]
    tracks-release-branches: false
    is-release-branch: false
    is-mainline: true
    pre-release-weight: 55000
  develop:
    regex: ^dev(elop)?(ment)?$
    mode: ContinuousDeployment
    tag: alpha
    increment: Minor
    prevent-increment-of-merged-branch-version: false
    track-merge-target: true
    source-branches: []
    tracks-release-branches: true
    is-release-branch: false
    is-mainline: false
    pre-release-weight: 0
  release:
    regex: ^releases?[/-]
    mode: ContinuousDelivery
    tag: beta
    increment: None
    prevent-increment-of-merged-branch-version: true
    track-merge-target: false
    source-branches: [ 'develop', 'main', 'support', 'release' ]
    tracks-release-branches: false
    is-release-branch: true
    is-mainline: false
    pre-release-weight: 30000
  feature:
    regex: ^features?[/-]
    mode: ContinuousDelivery
    tag: '{BranchName}'
    increment: Inherit
    source-branches: [ 'develop', 'main', 'release', 'feature', 'support', 'hotfix' ]
    pre-release-weight: 30000	
  pull-request:
    regex: ^(pull|pull\-requests|pr)[/-]
    mode: ContinuousDelivery
    tag: PullRequest
    increment: Inherit
    tag-number-pattern: '[/-](?<number>\d+)[-/]'
    source-branches: [ 'develop', 'main', 'release', 'feature', 'support', 'hotfix' ]
    pre-release-weight: 30000
  hotfix:
    regex: ^hotfix(es)?[/-]
    mode: ContinuousDelivery
    tag: beta
    increment: Inherit
    source-branches: [ 'release', 'main', 'support', 'hotfix' ]
    pre-release-weight: 30000
  support:
    regex: ^support[/-]
    mode: ContinuousDelivery
    tag: ''
    increment: Patch
    prevent-increment-of-merged-branch-version: true
    track-merge-target: false
    source-branches: [ 'main' ]
    tracks-release-branches: false
    is-release-branch: false
    is-mainline: true
    pre-release-weight: 55000
```

If you don't specify the regex, the built-in for that branch config will be
used (recommended).

We don't envision many people needing to change most of these configuration
values, but here they are if you need to:

### regex

This is the regex which is used to match the current branch to the correct
branch configuration.

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

*   feature/foo branches off release/v1.0.0
    *   Branch release/v1.0.0 from main
    *   Branch feature/foo from release/v1.0.0
    *   Add a commit to both release/v1.0.0 and feature/foo
    *   release/v1.0.0 is the base for feature/foo
*   release/v1.0.0 branches off feature/foo
    *   Branch feature/foo from main
    *   Branch release/v1.0.0 from feature/foo
    *   Add a commit to both release/v1.0.0 and feature/foo
    *   feature/foo is the base for release/v1.0.0

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
    regex: ...
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

### tag

The pre release tag to use for this branch. Use the value `useBranchName` to use
the branch name instead. For example `feature/foo` would become a pre-release
tag of `foo` with this value. Use the value `{BranchName}` as a placeholder to
insert the branch name. For example `feature/foo` would become a pre-release tag
of `alpha.foo` with the value of `alpha.{BranchName}`.

**Note:** To clear a default use an empty string: `tag: ''`

### increment

Same as for the [global configuration, explained above](#increment).

### prevent-increment-of-merged-branch-version

When `release-2.0.0` is merged into main, we want main to build `2.0.0`. If
`release-2.0.0` is merged into develop we want it to build `2.1.0`, this option
prevents incrementing after a versioned branch is merged.

In a GitFlow-based repository, setting this option can have implications on the
`CommitsSinceVersionSource` output variable. It can rule out a potentially
better version source proposed by the `MergeMessageBaseVersionStrategy`. For
more details and an in-depth analysis, please see [the discussion][2506].

### tag-number-pattern

Pull requests require us to extract the pre-release number out of the branch
name so `refs/pulls/534/merge` builds as `PullRequest.534`. This is a regex with
a named capture group called `number`.

If the branch `mode` is set to `ContinuousDeployment`, then the extracted
`number` is appended to the name of the pre-release tag and the number portion
is the number of commits since the last tag. This enables consecutive commits to
the pull request branch to generate unique full semantic version numbers when
the branch is configured to use ContinuousDeployment mode.

**Example usage:**

```yaml
branches:
  pull-request:
    mode: ContinuousDeployment
    tag: PullRequest
    increment: Inherit
    track-merge-target: true
    tag-number-pattern: '[/-](?<number>\d+)[-/]'
```

### track-merge-target

Strategy which will look for tagged merge commits directly off the current
branch. For example `develop` → `release/1.0.0` → merge into `main` and tag
`1.0.0`. The tag is _not_ on develop, but develop should be version `1.0.0` now.

### tracks-release-branches

Indicates this branch config represents develop in GitFlow.

### is-release-branch

Indicates this branch config represents a release branch in GitFlow.

### is-mainline

When using Mainline mode, this indicates that this branch is a mainline. By
default `main` and `support/*` are mainlines.

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

### semver-format

Specifies the semver format that is used when parsing the string.
Can be `Strict` - using the [regex](https://regex101.com/r/Ly7O1x/3/)
or `Loose` the old way of parsing. The default if not specified is `Strict`
Example of invalid `Strict`, but valid `Loose`

```
1.2-alpha4
01.02.03-rc03
1.2.3.4
```

[1145]: https://github.com/GitTools/GitVersion/issues/1145
[1366]: https://github.com/GitTools/GitVersion/issues/1366
[2506]: https://github.com/GitTools/GitVersion/pull/2506#issuecomment-754754037
[conventional-commits-config]: /docs/reference/version-increments#conventional-commit-messages
[conventional-commits]: https://www.conventionalcommits.org/
[installing]: /docs/usage/cli/installation
[modes]: /docs/reference/modes
[variables]: /docs/reference/variables
[version-sources]: /docs/reference/version-sources
