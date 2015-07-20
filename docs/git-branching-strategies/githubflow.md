# GitHub Flow
GitHub Flow is a simple and effective branching strategy which the folks at GitHub use. Most teams actually do not need everything GitFlow gives them and are much better off with a simpler workflow.

GitHub Flow is in a nutshell:

1) Update master to latest [upstream](../reference/git-setup.md#upstream) code
2) Create a feature branch `git checkout -b myFeatureBranch`
3) Do the feature/work
4) Push feature branch to [origin](../reference/git-setup.md#origin)
5) Create pull request from origin/<featureBranch> -> upstream/master
6) Review, fix raised comments, merge your PR or even better, get someone else to.

The main rule of GitHub Flow is that master should *always* be deployable. GitHub Flow allows and encourages [continuous deliver](../reference/continuous-delivery.md).

## Resources
 - [GitHubFlow guide by GitHub](https://guides.github.com/introduction/flow/index.html)
 - [GitHub Flow original blog post](http://scottchacon.com/2011/08/31/github-flow.html)
