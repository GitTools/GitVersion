v4.0.0
 - When using GitFlow, a few things have changed. Hopefully the new settings just work for you
    - develop has pre-release tag of `alpha` now, not unstable
    - develop will bump as soon as a release branch is created
    - Look at the [GitFlow examples](http://gitversion.readthedocs.io/en/latest/git-branching-strategies/gitflow-examples/) for details of how it works now
 - Regex's are no longer used as keys in branch config
    - We have named branches, and introduced a `regex` config which you can override.
    - The default keys are: master, develop, feature, release, pull-request, hotfix, support
    - Just run GitVersion.exe in your project directory and it will tell you what to change your config keys to
    - For example, `dev(elop)?(ment)?$` is now just `develop`, we suggest not overring regex's unless you really want to use a different convention.
 - source-branches added as a configuration option for branches, it helps GitVersion pick the correct source branch

v3.0.0
 - NextVersion.txt has been deprecated, only GitVersionConfig.yaml is supported
 - `AssemblyFileSemVer` variable removed, AssemblyVersioningScheme configuration value makes this variable obsolete
 - Variables `ClassicVersion` and `ClassicVersionWithTag` removed
 - MSBuild task arguments (AssemblyVersioningScheme, DevelopBranchTag, ReleaseBranchTag, TagPrefix, NextVersion) have been removed, use GitVersionConfig.yaml instead
 - GitVersionTask ReleaseDateAttribute no longer exists
