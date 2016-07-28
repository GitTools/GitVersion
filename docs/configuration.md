# Configuration
GitVersion 3.0 is mainly powered by configuration and no longer has branching strategies hard coded.

## Configuration tool
If you run `GitVersion init` you will be launched into a configuration tool, it can help you configure GitVersion the way you want it.

Once complete, the `init` command will create a `GitVersion.yml` file in the working directory. It can be the root repository directory or any subdirectory in case you have a single repository for more than one project or are restricted to commit into a subdirectory.

**Note:** GitVersion ships with internal default configuration which works with GitHubFlow and GitFlow, probably with others too.

The *develop* branch is set to `ContinuousDeployment` mode by default as we have found that is generally what is needed when using GitFlow.

You can run `GitVersion /showConfig` to see the effective configuration (defaults + overrides)

To create your config file just type `GitVersion init` in your repo directory after installing via chocolatey and we will create a sample (but commented out) config file.
Uncomment and modify as you need.

## Global configuration
The global configuration options are:

 - **`next-version:`** Allows you to bump the next version explicitly, useful for bumping `master` or a feature with breaking changes a major increment.

 - **`assembly-versioning-scheme:`** When updating assembly info tells GitVersion how to treat the `AssemblyVersion` attribute. Useful to lock the major when using Strong Naming. Note: you can use `None` to skip updating the `AssemblyVersion` while still updating the `AssemblyFileVersion` and `AssemblyInformationVersion` attributes.

 - **`assembly-informational-format:`** Set this to any of the available [variables](/more-info/variables) to change the value of the `AssemblyInformationalVersion` attribute. Default set to `{InformationalVersion}`. It also supports string interpolation (`{MajorMinorPatch}+{Branch}`)

 - **`mode:`** Sets the mode of how GitVersion should create a new version. Read more at [versioning mode](./versioning-mode.md)

 - **`continuous-delivery-fallback-tag:`** When using `mode: ContinuousDeployment`, the value specified will be used as the pre-release tag for branches which do not have one specified. Default set to `ci`.

 - **`tag-prefix:`** A regex which is used to trim git tags before processing (eg v1.0.0). Default is `[vV]` though this is just for illustrative purposes as we do a IgnoreCase match and could be `v`.

 - **`major-version-bump-message:`** The regex to match commit messages with to perform a major version increment. Default set to `'\+semver:\s?(breaking|major)'`, which will match occurrences of `+semver: major` and `+semver: breaking` in a commit message.

 - **`minor-version-bump-message:`** The regex to match commit messages with to perform a minor version increment. Default set to `'\+semver:\s?(feature|minor)'`, which will match occurrences of `+semver: feature` and `+semver: minor` in a commit message.

 - **`patch-version-bump-message:`** The regex to match commit messages with to perform a patch version increment. Default set to `'\+semver:\s?(fix|patch)'`, which will match occurrences of `+semver: fix` and `+semver: patch` in a commit message.

 - **`no-bump-message:`** Used to tell GitVersion not to increment when in Mainline development mode. Default `\+semver:\s?(none|skip)`, which will match occurrences of `+semver: none` and `+semver: skip`

 - **`legacy-semver-padding:`** The number of characters to pad `LegacySemVer` to  in the `LegacySemVerPadded` [variable](/more-info/variables). Is default set to `4`, which will pad the `LegacySemVer` value of `3.0.0-beta1` to `3.0.0-beta0001`.

 - **`build-metadata-padding:`** The number of characters to pad `BuildMetaData` to in the `BuildMetaDataPadded` [variable](/more-info/variables). Is default set to `4`, which will pad the `BuildMetaData` value of `1` to `0001`.

 - **`commits-since-version-source-padding:`** The number of characters to pad `CommitsSinceVersionSource` to in the `CommitsSinceVersionSourcePadded` [variable](/more-info/variables). Is default set to `4`, which will pad the `CommitsSinceVersionSource` value of `1` to `0001`.

 - **`commit-message-incrementing:`** Sets whether it should be possible to increment the version with special syntax in the commit message. See the `*-version-bump-message` options above for details on the syntax. Default set to `Enabled`; set to `Disabled` to disable.
 
 - **`ignore:`** The header for ignore configuration
   - **`sha:`** A sequence of SHAs to be excluded from the version calculations. Useful when there is a rogue commit in history yielding a bad version.
   - **`commits-before:`** Date and time in the format `yyyy-MM-ddTHH:mm:ss` (eg `commits-before: 2015-10-23T12:23:15`) to setup an exclusion range. Effectively any commit < `commits-before` will be ignored.

 - **`is-develop:`** Indicates this branch config represents develop in GitFlow

 **`is-release-branch:`** Indicates this branch config represents a release branch in GitFlow

## Branch configuration

Then we have branch specific configuration, which looks something like this:

```yaml
branches:
  master:
    tag:
    increment: Patch
    prevent-increment-of-merged-branch-version: true
  (pull|pull\-requests|pr)[/-]:
    tag: PullRequest
    increment: Inherit
    track-merge-target: true
    tag-number-pattern: '[/-](?<number>\d+)[-/]'
```

The options in here are:

 - **`branches:`** The header for all the individual branch configuration.

 - **`mode:`** Same as above

 - **`tag:`** The pre release tag to use for this branch.  
   Use the value `useBranchName` to use the branch name instead. For example `feature/foo` would become a pre-release tag of `foo` with this value.  
   Use the value `{BranchName}` as a placeholder to insert the branch name. For example `feature/foo` would become a pre-release tag of `alpha.foo` with the value of `alpha.{BranchName}`.  
   **Note:** To clear a default use an empty string: `tag: ""`

 - **`increment:`** the part of the SemVer to increment when GitVersion detects it needs to be (i.e commit after a tag)

 - **`prevent-increment-of-merged-branch-version:`** When `release-2.0.0` is merged into master, we want master to build `2.0.0`.
    If `release-2.0.0` is merged into develop we want it to build `2.1.0`, this option prevents incrementing after a versioned branch is merged

 - **`tag-number-pattern:`** Pull requests require us to extract the pre-release number out of the branch name so `refs/pulls/534/merge` builds as `PullRequest.534`.
   This is a regex with a named capture group called `number`  
   If the branch mode is set to ContinuousDeployment, then the extracted `number` is appended to the name of the pre-release tag and the number portion is the number of commits since the last tag.
   This enables consecutive commits to the pull request branch to generate unique full semantic version numbers when the branch is configured to use ContinuousDeployment mode.  
   Example usage:
```yaml
branches:
  (pull|pull\-requests|pr)[/-]:
    mode: ContinuousDeployment
    tag: PullRequest
    increment: Inherit
    track-merge-target: true
    tag-name-pattern: '[/-](?<number>\d+)[-/]'
```

 - **`track-merge-target:`** Strategy which will look for tagged merge commits directly off the current branch. For example `develop` → `release/1.0.0` → merge into `master` and tag `1.0.0`. The tag is *not* on develop, but develop should be version `1.0.0` now.

We don't envision many people needing to change most of these configuration values, but they are there if you need to.
