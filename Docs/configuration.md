# Configuration
GitVersion 3.0 is mainly powered by configuration and no longer has branching strategies hard coded.

## Configuration tool
If you run `GitVersion init` you will be launched into a configuration tool, it can help you configure GitVersion the way you want it.

**Note:** GitVersion ships with internal default configuration which works with GitHubFlow and GitFlow, probably with others too.

The *develop* branch is set to `ContinuousDeployment` mode by default as we have found that is generally what is needed when using GitFlow.

You can run `GitVersion /showConfig` to see the effective configuration (defaults + overrides)

To create your config file just type `GitVersion init` in your repo directory after installing via chocolatey and we will create a sample (but commented out) config file.
Uncomment and modify as you need.

The configuration options are:

 - `next-version`: Allows you to bump the next version explicitly, useful for bumping `master` or a feature with breaking changes a major increment.
 - `assembly-versioning-scheme`: When updating assembly info tells GitVersion how to treat the AssemblyVersion attribute. Useful to lock the major when using Strong Naming.
 - `mode`: Either ContinuousDelivery or ContinuousDeployment. See [Octopus Deploy/CI Build NuGet Packages](#continuousdeployment) above for more information
 - `continuous-delivery-fallback-tag`: When using `mode: ContinuousDeployment` the value specified will be used as the pre-release tag for branches which do not have one specified.
 - `tag-prefix`: A regex which is used to trim git tags before processing (eg v1.0.0). Default is `[vV]` though this is just for illustrative purposes as we do a IgnoreCase match and could be `v`

## Branch configuration

Then we have branch specific configuration, which looks something like this:

``` yaml
branches:
  master:
    tag:
    increment: Patch
    prevent-increment-of-merged-branch-version: true
  (pull|pull\-requests|pr)[/-]:
    tag: PullRequest
    increment: Inherit
    tag-number-pattern: '[/-](?<number>\d+)[-/]'
```

The options in here are:
 - `mode`: Same as above
 - `tag`: The pre release tag to use for this branch. Use the value `use-branch-name-as-tag` to use the branch name instead.  
   For example `feature/foo` would become a pre-release tag of `foo` with this value
 - `increment`: the part of the SemVer to increment when GitVersion detects it needs to be (i.e commit after a tag)
 - `prevent-increment-of-merged-branch-version`: When `release-2.0.0` is merged into master, we want master to build `2.0.0`.
    If `release-2.0.0` is merged into develop we want it to build `2.1.0`, this option prevents incrementing after a versioned branch is merged
 - `tag-number-pattern`: Pull requests require us to pull the pre-release number out of the branch name so `refs/pulls/534/merge` builds as `PullRequest.5`.
   This is a regex with a named capture group called `number`
 - `track-merge-target`: Strategy which will look for tagged merge commits directly off the current branch. For example
   develop -> release/1.0.0 -> merge into master and tag 1.0.0. The tag is *not* on develop, but develop should be 1.0.0 now.

We don't envision many people needing to change most of these configuration values, but they are there if you need to.
