## Unreleased

*   When using a commit message that matches **both** `*-version-bump-message` and `no-bump-message`, there is no increment for that commit. In other words, `no-bump-message` now takes precedence over `*-version-bump-message`.
*   The fallback version strategy now returns `0.0.0` and is flagged with `ShouldIncrement` equal to `true`. This yields the version `0.1.0` on the `develop` branch (`IncrementStrategy.Minor` by default) and `0.0.1` on the `main` branch (`IncremetnStrategy.Patch` by default).
*   The current branch (child) inherits its configuration from the source (parent) branch if the `increment` strategy is set to `Inherit`. This makes branch configuration recursive, simpler, more intuitive, more flexible, and more robust.
*   Instead of having a single effective configuration, we now have one effective configuration per branch where the increment strategy is not set to `inherit`.
*   The new implementation of the branch configuration inheritance affects per default only the pull-requests, hotfix and feature branches. In this case the next version will be generated like the child branch is not existing and the commits have been made on the source branch.
    *   The following example illustrates this behavior. On the feature branch the semantic version `1.1.0-just-a-test.1+2` will now be generated instead of version `1.0.0-just-a-test.1+3` previously:
	```
    * 1f1cfb4 52 minutes ago  (HEAD -> feature/just-a-test)
	* 1f9654d 54 minutes ago  (release/1.1.0)
	* be72411 56 minutes ago  (develop)
	* 14800ff 58 minutes ago  (tag: 1.0.0, main)
	```

## v5.0.0

*   Version numbers in branches other than `release` branches are no longer
    considered as a version source by default. Implemented in [#1541][pr-1541].
*   [#1581][pr-1581] folds `GitTools.Core` back into GitVersion to make
    maintaining GitVersion easier.

## v4.0.0

### Git Flow Changes

When using GitFlow, a few things have changed. Hopefully the new settings just
work for you

*   `develop` has pre-release tag of `alpha` now, not unstable.
*   `develop` will bump as soon as a `release` branch is created.
*   Look at the [GitFlow examples][gitflow] for details of how it works now.

### Configuration Changes

*   `GitVersionConfig.yaml` is deprecated in favor of `GitVersion.yml`.
*   Regular expressions are no longer used as keys in branch config
    *   We have named branches, and introduced a `regex` config which you can
        override.
    *   The default keys are: `master`, `develop`, `feature`, `release`, `pull-request`,
        `hotfix` and `support`
    *   Just run `GitVersion.exe` in your project directory and it will tell you
        what to change your config keys to
    *   For example, `dev(elop)?(ment)?$` is now just `develop`, we suggest not
        overring regular expressions unless you really want to use a different convention.
*   `source-branches` added as a configuration option for branches, it helps
    GitVersion pick the correct source branch

## v3.0.0

*   NextVersion.txt has been deprecated, only `GitVersionConfig.yaml` is supported
*   `AssemblyFileSemVer` variable removed, `AssemblyVersioningScheme` configuration
    value makes this variable obsolete
*   Variables `ClassicVersion` and `ClassicVersionWithTag` removed
*   MSBuild task arguments (`AssemblyVersioningScheme`, `DevelopBranchTag`,
    `ReleaseBranchTag`, `TagPrefix`, `NextVersion`) have been removed, use
    `GitVersionConfig.yaml` instead
*   GitVersionTask's `ReleaseDateAttribute` no longer exists

[gitflow]: https://gitversion.net/docs/learn/branching-strategies/gitflow-examples_complete
[pr-1541]: https://github.com/GitTools/GitVersion/pull/1541
[pr-1581]: https://github.com/GitTools/GitVersion/pull/1581
