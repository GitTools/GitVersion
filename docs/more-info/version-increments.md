# Version Incrementing
Because GitVersion works with a number of workflows the way it does its version incrementing may work perfectly for you, or it may cause you issues. This page is split up into two sections, first is all about understanding the approach GitVersion uses by default, and the second is how you can manually increment the version.

## Approach
Semantic Versioning is all about *releases*, not builds. This means that the version only increases after you release, this directly conflicts with the concept of published CI builds. When you release the next version of your library/app/website/whatever you should only increment major/minor or patch then reset all lower parts to 0, for instance given 1.0.0, the next release should be either `2.0.0`, `1.1.0` or `1.0.1`. Bumping one of the version components by more than 1 in a single release means you will have gaps in your version number, which defeats the purpose of SemVer.

Because of this, GitVersion works out what the next SemVer of your app is on each commit. When you are ready to release you simply deploy the latest built version and tag the release it was from. This practice is called **continuous delivery**. GitVersion will increment the *metadata* for each build so you can tell builds apart. For example `1.0.0+5` followed by `1.0.0+6`. It is important to note that build metadata *is not part of the semantic version, it is just metadata!*.

All this effectively means that GitVersion will produce the same version NuGet package each commit until you tag a release.

This causes problems for people as NuGet and other package managers do not support multiple packages with the same version with only different metadata.
There are a few ways to handle this problem depending on what your requirements are:

### 1. GitFlow
If you are using GitFlow then builds off the `develop` branch will actually *increment on every commit*. This is known in GitVersion as *continuous deployment mode*. By default `develop` builds are tagged with the `alpha` pre-release tag. This is so they are sorted higher than release branches.

If you need to consume packages built from develop, we recommend publishing these packages to a separate NuGet feed as an alpha channel. That way you can publish beta/release candidate builds and only people who opt into the alpha feed will see the alpha packages.

### 2. Octopus deploy
See [Octopus deploy](../build-server-support/build-server/octopus-deploy.md)

## Manually incrementing the version
With v3 there are multiple approaches.

### Commit messages
Adding `+semver: breaking` or `+semver: major` will cause the major version to be increased, `+semver: feature` or `+semver:minor` will bump minor and `+semver:patch` or `+semver:fix` will bump the patch.

#### Configuration
The feature is enabled by default but can be disabled via configuration, the regex we use can be changed:

```
major-version-bump-message: '\+semver:\s?(breaking|major)'
minor-version-bump-message: '\+semver:\s?(feature|minor)'
patch-version-bump-message: '\+semver:\s?(fix|patch)'
commit-message-incrementing: Enabled
```

The options for `commit-message-incrementing` are `Enabled`, `MergeMessageOnly` and `Disabled`

If the incrementing mode is set to `MergeMessageOnly` you can add this information in when merging a pull request. This prevents commits within a PR bumping the version.

### GitVersion.yml
The first is by setting the `next-version` property in the GitVersion.yml file. This property only serves as a base version,

### Branch name
If you create a branch with the version number in the branch name such as `release-1.2.0` or `hotfix/1.0.1` then GitVersion will take the version number from the branch name as a source

### Tagging commit
By tagging a commit, GitVersion will use that tag for the version of that commit, then increment the next commit automatically based on the increment rules for that branch (some branches bump patch, some minor).
