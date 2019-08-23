Versioning when using Git, solved. GitVersion looks at your Git history and works out the [semantic version](http://semver.org) of the commit being built.

It works with most branching strategies but has been designed mainly around GitFlow and GitHubFlow (pull request workflow). The calculated version numbers can then be accessed through variables such as `$(GitVersion.NuGetVersion)` and `$(GitVersion.SemVer)`. It is also very configurable to allow it to work with most release workflows!

![Build Task](https://raw.githubusercontent.com/GitTools/GitVersion/master/src/GitVersionTfsTask/images/build-task.png)
![Builds](https://raw.githubusercontent.com/GitTools/GitVersion/master/src/GitVersionTfsTask/images/builds.png)
