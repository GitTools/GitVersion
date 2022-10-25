---
Order: 50
Title: FAQ
RedirectFrom: docs/faq
---

## Why is my version not incrementing?

GitVersion calculates the semantic version, this will only change once per
_release_. Read more about [version increments][increments].

## I'm using Octopus deploy

Because Octopus deploy cannot have the same version of a package to a NuGet
feed. There is no magic solution to this, but you can read more about your
options at [octopus deploy][octopus].

## How can GitVersion run for a shallow clone or checkout on server working directories

GitVersion needs a proper git repository to run, some build servers do not do a
proper clone which can cause issues. GitVersion has a feature called [dynamic
repositories][dynamic-repos] which solves this by cloning the repository and
working against that clone instead of the working directory.

## I don't understand what SemVer is all about

Not a problem, we have a quick [introduction to SemVer][semver-intro] which can
be a good primer to read before reading [SemVer.org][semver].

## I can't use the build number for NuGet

If you have used NuGet you would notice the versions above are not compatible
with NuGet. GitVersion solves this by providing [variables][variables].

What you have seen above is the `SemVer` variable. You can use the
`NuGetVersion` variable to have the version formatted in a NuGet compatible way.

So `1.0.1-rc.1+5` would become `1.0.1-rc0001`, this takes into account
characters which are not allowed and NuGets crap sorting.

:::{.alert .alert-info}
**Note**

The `NuGetVersion` variable is floating, so when NuGet 3.0 comes out
with proper SemVer support GitVersion will switch this variable to a proper
SemVer.
:::

If you want to fix the version, use `NuGetVersionV2` which will stay the same
after NuGet 3.0 comes out

## How do I choose my branching strategy (GitFlow vs GitHubFlow)

If you run `gitversion init` then choose `Getting started wizard` then choose
`Unsure, tell me more`, GitVersion will run through a series of questions which
will try and help point you towards a branching strategy and why you would use
it.

## Merged branch names as version source

When GitVersion considers previous commits to calculate a version number, it's
important that the metadata to be considered is _stable_. Since branches are
usually deleted after they are merged, the name of a branch can't be considered
as a stable version source. _Branch names are not stable_, they are ephemeral.

The only place a branch name can be considered for version calculation is for
the branch itself. This is typically used for `release/*` branches, which
usually have a version number in their name. For the release branch
`release/1.2.3`, the verison number `1.2.3` will be used to calculate the final
version number _for the release branch_.

However, when the `release/1.2.3` branch is merged into `main`, the fact that
the merged commits came from a branch named `release/1.2.3` vanishes with the
branch which will be deleted. The name of the merged release branch can
therefore not be considered for version calculation in the target branch of the
merge.

[dynamic-repos]: /docs/learn/dynamic-repositories
[increments]: /docs/reference/version-increments
[octopus]: /docs/reference/build-servers/octopus-deploy
[semver-intro]: /docs/learn/intro-to-semver
[semver]: https://semver.org
[variables]: /docs/reference/variables
