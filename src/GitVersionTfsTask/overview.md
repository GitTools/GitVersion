Versioning when using git, solved. GitVersion looks at your git history and works out the semantic version (semver.org) of the commit being built.

It works with most branching strategies but has been designed mainly around GitFlow and GitHubFlow (pull request workflow). The calculated version numbers can then be accessed through variables such as `$(GitVersion_NuGetVersion)` and `$(GitVersion_SemVer)`. It is also very configurable to allow it to work with most release workflows!

![Build task](img/build-task.png)

![Builds](img/builds.png)