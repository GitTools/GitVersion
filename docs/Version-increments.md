# Version Incrementing
Because GitVersion works with a number of workflows the way it does it's version incrementing may work perfectly for you, or it may cause you issues. This page is split up into two sections, first is all about understanding the approach GitVersion uses by default, and the second is how you can manually increment the version.

## Approach
Semantic Versioning is all about *releases*, not builds. This means that the version only increases after you release, this directly conflicts with the concept of published CI builds. When you release the next version of your library/app/website/whatever you should only increment major/minor or patch then reset all lower parts to 0, for instance given 1.0.0, the next release should be either `2.0.0`, `1.1.0` or `1.0.1`. Bumping one of the version components by more than 1 in a single release means you will have gaps in your version number, which defeats the purpose of SemVer.

Because of this, GitVersion works out what the next SemVer of your app is on each commit. When you are ready to release you simply deploy the latest built version and tag the release it was from. This practice is called **continuous delivery**. GitVersion will increment the *metadata* for each build so you can tell builds apart. For example `1.0.0+5` followed by `1.0.0+6`. It is important to note that build metadata *is not part of the semantic version, it is just metadata!*.

All this effectively means that GitVersion will produce the same version NuGet package each commit until you tag a release.

This causes problems for people as NuGet and other package managers do not support multiple packages with the same version with only different metadata.
There are a few ways to handle this problem depending on what your requirements are:

### 1. GitFlow
If you are using GitFlow then builds off the `develop` branch will actually *increment on every commit*. This is known in GitVersion as *continuous deployment mode*. By default `develop` builds are tagged with the `unstable` pre-release tag. This is so they are sorted higher than release branches.

If you need to consume packages built from develop, we recommend publishing these packages to a separate NuGet feed as an alpha channel. That way you can publish beta/release candidate builds and only people who opt into the alpha feed will see the unstable pacakges.

### 2. Octopus deploy
Because Octopus uses NuGet under the covers you cannot publish every build into Octopus deploy. For this we have two possible options:

#### 2a. 'Release' packages to Octopus deploy
Rather than all builds going into Octopus's NuGet feed, you release builds into it's feed. When you push a package into the NuGet feed you need to tag that release. The next commit will then increment the version.
This has the advantage that if you have a multi-stage deployment pipeline you pick packages which you would like to start through the pipeline, then you can see all the versions which did not make it through the pipeline (for instance, they got to UAT but not production due to a bug being found). In the release notes this can be mentioned or those versions can be skipped.

#### 2b. Configure GitVersion to increment per commit
As mentioned above, this means you will burn multiple versions per release. This might not be an issue for you, but can confuse consumers of your library as the version has semantic meaning.

## Manually incrementing the version
With v3 there are multiple approaches.

### GitVersionConfig.yaml
The first is by setting the `next-version` property in the GitVersionConfig.yaml file. This property only serves as a base version,

### Branch name
If you create a branch with the version number in the branch name such as `release-1.2.0` or `hotfix/1.0.1` then GitVersion will take the version number from the branch name as a source

### Tagging commit
By tagging a commit, GitVersion will use that tag for the version of that commit, then increment the next commit automatically based on the increment rules for that branch (some branches bump patch, some minor).
