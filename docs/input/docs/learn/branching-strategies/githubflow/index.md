---
Order: 30
Title: GitHub Flow
Description: GitHub flow is a simpler and pull request-driven branching strategy
RedirectFrom: docs/git-branching-strategies/githubflow
---

GitHubFlow is a simple and effective branching strategy which the folks at
GitHub use. Most teams actually do not need everything GitFlow gives them and
are much better off with a simpler workflow.

GitHubFlow is in a nutshell:

1. Update main to latest [upstream][upstream] code
2. Create a feature branch `git checkout -b myFeatureBranch`
3. Do the feature/work
4. Push feature branch to [origin][origin]
5. Create pull request from origin/{featureBranch} -> upstream/main
6. Review, fix raised comments, merge your PR or even better, get someone else to.

The main rule of GitHub Flow is that main should _always_ be deployable.
GitHub Flow allows and encourages [continuous delivery][continuous-delivery].

## Resources

* [GitHubFlow guide by GitHub][githubflow-guide-by-github]
* [GitHubFlow original blog post][githubflow-original-blog-post]
* [Phil Haack's (haacked) GitHubFlow aliases][phil-haack-s-haacked-githubflow-aliases]
* [GitHubFlow vs GitFlow][githubflow-vs-gitflow]

[upstream]: /docs/learn/git-setup#upstream

[origin]: /docs/learn/git-setup#origin

[continuous-delivery]: /docs/reference/modes/continuous-delivery

[githubflow-guide-by-github]: https://docs.github.com/en/get-started/quickstart/github-flow#introduction

[githubflow-original-blog-post]: https://scottchacon.com/2011/08/31/github-flow

[phil-haack-s-haacked-githubflow-aliases]: https://haacked.com/archive/2014/07/28/github-flow-aliases/

[githubflow-vs-gitflow]: https://lucamezzalira.com/2014/03/10/git-flow-vs-github-flow/
