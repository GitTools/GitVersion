---
Order: 50
Title: GitHubFlow Examples
RedirectFrom: docs/git-branching-strategies/githubflow-examples
---

## Feature branch

![GitHubFlow](/docs/img/githubflow_feature-branch.png)

## Pull requests

![GitHubFlow](/docs/img/githubflow_pull-request.png)

## Release branch

Release branches can be used in GitHubFlow as well as GitFlow. Sometimes you
want to start on a large feature which may take a while to stabilize so you want
to keep it off main. In these scenarios you can either create a long lived
feature branch (if you do not know the version number this large feature will go
into, and it's non-breaking) otherwise you can create a release branch for the
next major version. You can then submit pull requests to the long lived feature
branch or the release branch.

![GitFlow](/docs/img/githubflow_release-branch.png)
