## Unreleased

### Logging System Replacement

* The custom `ILog` logging abstraction has been replaced with the industry-standard `Microsoft.Extensions.Logging` (M.E.L.) infrastructure using Serilog as the underlying provider.

* **Removed public types** from `GitVersion.Logging` namespace:
  * `ILog` interface
  * `ILogAppender` interface
  * `LogLevel` enum
  * `LogAction` delegate
  * `LogActionEntry` delegate
  * `LogExtensions` class

* **Migration for custom integrations**:
  * If you were injecting `ILog`, inject `ILogger<T>` instead
  * If you implemented `ILogAppender`, implement `ILoggerProvider` instead
  * The `Verbosity` enum is preserved for CLI usage and maps to Serilog log levels internally

* **Preserved types**:
  * `Verbosity` enum (Quiet/Minimal/Normal/Verbose/Diagnostic) - still used for CLI verbosity control
  * `IConsole` interface - moved from `GitVersion.Logging` to `GitVersion` namespace

## v6.2.0

* The configuration property `label-number-pattern` was removed. The functionality can be still used by changing the label and the branch name regular expression for pull-request branches.

## v6.0.0

### Platforms

* Drop support for .NET Framework 4.8, .NET Core 3.1 and .NET 5.0. Changed the project targets to .NET 6.0 or later.

### Caching

* Refactor caching system in GitVersion to use json files instead of yaml files. This change is not backwards compatible with the old caching system.

### Configuration changes:

* The configuration properties `continuous-delivery-fallback-tag`, `tag-number-pattern`, and `tag` were renamed to `continuous-delivery-fallback-label`, `label-number-pattern`, and `label` respectively. `tag-pre-release-weight` and `tag-prefix` remained as they were as they are referring to a Git tag.

* When using a commit message that matches **both** `*-version-bump-message` and `no-bump-message`, there is no increment for that commit. In other words, `no-bump-message` now takes precedence over `*-version-bump-message`.

* The fallback version strategy now returns `0.0.0` and is flagged with `ShouldIncrement` equal to `true`. This yields the version `0.1.0` on the `develop` branch (`IncrementStrategy.Minor` by default) and `0.0.1` on the `main` branch (`IncremetnStrategy.Patch` by default).

* The current branch (child) inherits its configuration from the source (parent) branch if the `increment` strategy is set to `Inherit`. This makes branch configuration recursive, simpler, more intuitive, more flexible, and more robust.

* Instead of having a single effective configuration, we now have one effective configuration per branch where the increment strategy is not set to `inherit`.

* The new implementation of the branch configuration inheritance affects per default only the pull-requests, hotfix and feature branches. In this case the next version will be generated like the child branch is not existing and the commits have been made on the source branch.
  * The following example illustrates this behavior. On the feature branch the semantic version `1.1.0-just-a-test.1+2` will now be generated instead of version `1.0.0-just-a-test.1+3` previously:

    ```log
    * 1f1cfb4 52 minutes ago  (HEAD -> feature/just-a-test)
    * 1f9654d 54 minutes ago  (release/1.1.0)
    * be72411 56 minutes ago  (develop)
    * 14800ff 58 minutes ago  (tag: 1.0.0, main)
    ```

* A new `unknown` branch magic string has been introduced to give the user the possibility to specify the branch configuration for a branch which is not known. A branch is not known if only the regular expression of the branch configuration with the name `unknown` is matching. Please notice that this branch configuration behaves like any other branch configurations.

* Additional `fallback` branch configuration properties have been introduced at the root to define base properties which will be inherit to the branch configurations. That means if no other branch configuration in the inheritance line defines the given property the fallback property applies. Notice that the inheritance tree can be controlled using the increment strategy property in the branch configuration section.
  * The following example illustrates this behavior. The hotfix branch configuration overrides the main branch configuration and the result overrides the fallback branch configuration.

    ```log
    * 1f1cfb4 52 minutes ago  (HEAD -> hotfix/just-a-test)
    * 14800ff 58 minutes ago  (tag: 1.0.0, main)
    ```

* When overriding the configuration with e.g. GitVersion.yaml the software distinguishes between properties who are not existent and properties who are `null`. This is especially important if the user wants to define branch related configuration which are marked with `increment` strategy `Inherit`.

* Following root configuration properties have been removed:
  * continuous-delivery-fallback-tag

* A new branch related property with name `track-merge-message` has been introduced. Consider we have a `main` branch and a `release/1.0.0` branch and merge changes from `release/1.0.0` to the main branch. In this scenario the merge message will be interpreted as a next version `1.0.0` when `track-merge-message` is set to `true` otherwise `0.0.1`.

* The pre-release tags are only considered when they are matching with the label name of the branch. This has an effect on the way how the `CommitCountSource` will be determined.

* The process of increasing the version with bump message when `CommitMessageIncrementing` is enabled and increment strategy is `None` has been changed.

* A new configuration property with name `version-in-branch-pattern` has been introduced. This setting only applies on branches where the option `is-release-branch` is set to `true`. Please notice that the branch name needs to be defined after the version number by default (instead of `support/lts-2.0.0` please name the branch like `support/2.0.0-lts`).

* The `is-release-branch` property of the `hotfix` branch setting has been changed from `false` to `true`. If present the hotfix number will be considered now by default.

* In the GitHub and the Git Flow workflows the `label` property is by default set to an empty string on the `main` branch. This yields to a pre-release version on `main` with an empty tag. Instead of for instance `1.0.1+46` GitVersion generates the full semantic version `1.0.1-46` instead. This behavior can be changed to generate only stable versions (no pre-release version) with setting the label to `null` (Please keep in mind that the `label` property on root needs to be set to `null` as well, otherwise the fallback applies). This change is caused by issue #2347.

* The `useBranchName` magic string has been removed. Instead use `{BranchName}` for `label`.

* The `BranchPrefixToTrim` configuration property has been removed. `RegularExpression` is now used to capture named groups instead.
  * Default `RegularExpression` for feature branches is changed from `^features?[\/-]` to `^features?[\/-](?<BranchName>.+)` to support using `{BranchName}` out-of-the-box
  * Default `RegularExpression` for unknown branches is changed from `.*` to `(?<BranchName>.+)` to support using `{BranchName}` out-of-the-box

* The `Mainline` mode and the related implementation has been removed completely. The new `Mainline` version strategy should be used instead.

* The `Mainline` version strategy doesn't support downgrading the increment for calculating the next version. This is the case if e.g. a bump messages has been defined which is lower than the branch increment.

* The branch related property `is-mainline` in the configuration system has been renamed to `is-main-branch`

* The versioning mode has been renamed to deployment mode and consists of following values:
  * ManualDeployment (previously ContinuousDelivery)
  * ContinuousDelivery (previously ContinuousDeployment)
  * ContinuousDeployment (new)

* At the configuration root level, a new array called `strategies` has been introduced, which can consist of on or more following values:
  * ConfiguredNextVersion
  * MergeMessage
  * TaggedCommit
  * TrackReleaseBranches
  * VersionInBranchName
  * Mainline

* The initialization wizard has been removed.

* On the `develop`, `release` and `hotfix` branch the introduced branch related property `prevent-increment.when-current-commit-tagged` has been set to `false` to get the incremented instead of the tagged semantic version.

* When setting the "ignore commits before" parameter to a future value, an exception will occur if no commits are found on the current branch. This behavior mimics that of an empty repository.

* On the `GitFlow` workflow the increment property has been changed:
  * in branch `release` from `None` to `Minor` and
  * in branch `hotfix` from `None` to `Patch`

* On the `GitHubFlow` workflow the increment property has been changed in branch `release` from `None` to `Patch`.

* When creating a branch with name `hotfix/next` (by using the `GitFlow` workflow) or `release/next` (by the `GitHubFlow` workflow) the resulting version will yield to a patched version per default.

* If you have a tag `1.0.0` on `main` and branch from `main` to `release/1.0.1` then the next version number will be `1.1.0` when using the `GitFlow` workflow. This behavior is expected (but different compared to the `GitHubFlow` workflow) because on the `GitFlow` workflow you have an addition branch configuration with name hotfix where `is-release-branch` is set to `true`. That means if you want `1.0.1` as a next version you need to branch to `hotfix/1.0.1` or `hotfix/next`.  On the other hand if you use the `GitHubFlow` workflow the next version number will be `1.0.1` because the increment on the `release` branch is set to `Patch`.

* There is a new configuration parameter `semantic-version-format` with default of `Strict`. The behavior of `Strict` is, that every possible non-semver version e.g. `1.2.3.4` is ignored when trying to calculate the next version. So, if you have three-part and four-part version numbers mixed, it will compute the next version on basis of the last found three-part version number, ignoring all four-part numbers.
This is different compared to v5 where per default it was a `Loose` comparison.

### Legacy Output Variables

The following legacy output variables have been removed in this version:

* `BuildMetaDataPadded`
* `LegacySemVer`
* `LegacySemVerPadded`
* `NuGetVersionV2`
* `NuGetVersion`
* `NuGetPreReleaseTagV2`
* `NuGetPreReleaseTag`
* `CommitsSinceVersionSourcePadded`

## v5.0.0

* Version numbers in branches other than `release` branches are no longer
  considered as a version source by default. Implemented in [#1541][pr-1541].
* [#1581][pr-1581] folds `GitTools.Core` back into GitVersion to make
  maintaining GitVersion easier.

## v4.0.0

### Git Flow Changes

When using GitFlow, a few things have changed. Hopefully the new settings just
work for you

* `develop` has pre-release tag of `alpha` now, not unstable.
* `develop` will bump as soon as a `release` branch is created.
* Look at the [GitFlow examples][gitflow] for details of how it works now.

### Configuration Changes

* `GitVersionConfig.yaml` is deprecated in favor of `GitVersion.yml`.
* Regular expressions are no longer used as keys in branch config
  * We have named branches, and introduced a `regex` config which you can
    override.
  * The default keys are: `master`, `develop`, `feature`, `release`, `pull-request`,
    `hotfix` and `support`
  * Just run `GitVersion.exe` in your project directory and it will tell you
    what to change your config keys to
  * For example, `dev(elop)?(ment)?$` is now just `develop`, we suggest not
    overring regular expressions unless you really want to use a different convention.
* `source-branches` added as a configuration option for branches, it helps
  GitVersion pick the correct source branch

## v3.0.0

* NextVersion.txt has been deprecated, only `GitVersionConfig.yaml` is supported
* `AssemblyFileSemVer` variable removed, `AssemblyVersioningScheme` configuration
  value makes this variable obsolete
* Variables `ClassicVersion` and `ClassicVersionWithTag` removed
* MSBuild task arguments (`AssemblyVersioningScheme`, `DevelopBranchTag`,
  `ReleaseBranchTag`, `TagPrefix`, `NextVersion`) have been removed, use
  `GitVersionConfig.yaml` instead
* GitVersionTask's `ReleaseDateAttribute` no longer exists

[gitflow]: https://gitversion.net/docs/learn/branching-strategies/gitflow-examples_complete

[pr-1541]: https://github.com/GitTools/GitVersion/pull/1541

[pr-1581]: https://github.com/GitTools/GitVersion/pull/1581
