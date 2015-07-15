# FAQ

## Why is my version not incrementing?
GitVersion calculates the semantic version, this will only change once per *release*. Read more at [version increments](./versionIncrements.md)

## I can't use the build number for NuGet
If you have used NuGet you would notice the versions above are not compatible with NuGet. GitVersion solves this by providing *variables*.

What you have seen above is the **SemVer** variable. You can use the **NuGetVersion** variable to have the version formatted in a NuGet compatible way.
So `1.0.1-rc.1+5` would become `1.0.1-rc0001`, this takes into account characters which are not allowed and NuGets crap sorting.

**note: ** The `NuGetVersion` variable is floating, so when NuGet 3.0 comes out with proper SemVer support GitVersion will switch this variable to a proper SemVer.
If you want to fix the version, use `NuGetVersionV2` which will stay the same after NuGet 3.0 comes out

## How do I choose my branching strategy (GitFlow vs GitHubFlow)
If you run `gitversion init` then choose `Getting started wizard` then choose `Unsure, tell me more`, GitVersion will run through a series of questions which will try and help point you towards a branching strategy and why you would use it.
