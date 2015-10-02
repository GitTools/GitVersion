# GitHub Flow
GitHub Flow is a simple and effective branching strategy which the folks at GitHub use. Most teams actually do not need everything GitFlow gives them and are much better off with a simpler workflow.

GitHub Flow is in a nutshell:

1. Update master to latest [upstream](../reference/git-setup.md#upstream) code  
1. Create a feature branch `git checkout -b myFeatureBranch`  
1. Do the feature/work  
1. Push feature branch to [origin](../reference/git-setup.md#origin)  
1. Create pull request from origin/<featureBranch> -> upstream/master  
1. Review, fix raised comments, merge your PR or even better, get someone else to.  

The main rule of GitHub Flow is that master should *always* be deployable. GitHub Flow allows and encourages [continuous delivery](../reference/continuous-delivery.md).

## Resources
 - [GitHubFlow guide by GitHub](https://guides.github.com/introduction/flow/index.html)
 - [GitHub Flow original blog post](http://scottchacon.com/2011/08/31/github-flow.html)
 - [Phil Haack's (haacked) GitHubFlow aliases](http://haacked.com/archive/2014/07/28/github-flow-aliases/)
 - [GitHubFlow vs GitFlow](http://lucamezzalira.com/2014/03/10/git-flow-vs-github-flow/)
