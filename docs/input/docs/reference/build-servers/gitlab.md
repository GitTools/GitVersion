---
Order: 80
Title: GitLab CI
Description: Details on the GitLab CI support in GitVersion
RedirectFrom: docs/build-server-support/build-server/gitlab
---

To use GitVersion with GitLab CI, either use the [MSBuild
Task](/docs/usage/msbuild) or put the GitVersion executable in your
runner's `PATH`.

### Tag pipelines

GitVersion uses GitLab's [`CI_COMMIT_TAG` predefined variable](https://docs.gitlab.com/ci/variables/predefined_variables/)
to identify the selected tag. This variable contains the tag name, such as
`1.2.3`, rather than the full `refs/tags/1.2.3` reference. When it is nonempty,
GitVersion treats the build as a tag pipeline rather than using
`CI_COMMIT_REF_NAME` as a branch name.

Rerunning a historical tag pipeline after the branch has advanced can leave HEAD
detached at a commit that is no longer the tip of any branch. If normalization
cannot attach HEAD to a local branch, it checks the selected local tag. When
that tag resolves to the checked-out commit, GitVersion keeps HEAD detached and
avoids a remote lookup that could be ambiguous because GitLab advertises
`refs/pipelines/*` and `refs/environments/*/deployments/*` at the same commit.
Existing branch selection takes precedence when a local branch identifies HEAD.

The selected tag must exist locally and resolve to HEAD for this behavior to
apply. A missing or empty `CI_COMMIT_TAG`, a missing local tag, or a tag pointing
to a different commit does not bypass normal ref discovery. An unrelated local
tag does not identify the build. This behavior does not replace the full Git
history required for version calculation.

### Merge Request pipelines

In merge request pipelines GitLab sets `CI_MERGE_REQUEST_REF_PATH` (for example
`refs/merge-requests/15/head` or `refs/merge-requests/15/merge`). GitVersion
reads this variable through the `GitLabCi` build agent when it is present,
following the same pass-through pattern as Azure Pipelines (`BUILD_SOURCEBRANCH`)
and GitHub Actions (`GITHUB_REF`).

Branch resolution order in `GitLabCi`:

1. `CI_COMMIT_TAG` set — treat as a tag pipeline (`GetCurrentBranch` returns `null`)
2. `CI_MERGE_REQUEST_REF_PATH` set — use the merge request ref
3. otherwise — `CI_COMMIT_REF_NAME` (branch name)

After repository normalisation the friendly branch name becomes
`merge-requests/<iid>/head` or `merge-requests/<iid>/merge`. Extend the
`pull-request` branch configuration in `GitVersion.yml` so the regex matches
GitLab's namespace (the default `pull-requests|pull|pr` pattern does not):

```yaml
workflow: GitFlow/v1
calculation:
  branches:
    pull-request:
      label: PullRequest{Number}
      regex: ^merge-requests/(?<Number>\d+)/(head|merge)$
```

`CI_COMMIT_REF_NAME` still contains the source branch name (for example
`feature/foo`) in MR pipelines; it is ignored when `CI_MERGE_REQUEST_REF_PATH`
is set.

A working example of integrating GitVersion with GitLab is maintained in the project [Utterly Automated Versioning][utterly-automated-versioning]

Here is a summary of what it demonstrated (many more details in the [Readme][readme])

* Is a reusable working example known as a Guided Exploration ([Guided Exploration Manifesto][guided-exploration-manifesto]) - so job logs and package artifacts can be reviewed. The project can also be imported to your own GitLab group or instance as a starting point for your own work.
* IMPORTANT: It demonstrates how to override GitLab CI's default cloning behavior so that GitVersion can do a dynamic copy. Selectively clones GitVersion.yml so that these settings take effect. This best practice demonstrates the best way to do this while avoiding a double-cloning of the project (once by GitLab Runner and once by GitVersion).
* Implements GitVersion as a CI/CD Extension that can be reused across many projects using includes.
* Implements GitVersion as a single job that runs the GitVersion container and passes the version number downstream into both _pipeline_ and _job_ level variables, which means...
* It can be used with ANY coding language, framework or packaging engine.
* Generates example packaged artifacts:
  * Two ways of building Sem Versioned NuGet packages (msbuild-ish and nuget.exe-ish) and uploads them and tests them from a [GitLab NuGet Repository][gitlab-nuget-repository].
  * A Sem Versioned [GitLab Generic Package][gitlab-generic-package]
  * A Sem Versioned docker container and uploads to [GitLab Container Registry][gitlab-container-registry].
* It creates a Sem Versioned [GitLab Release][gitlab-release-help] and Git tag using the [GitLab Release Cli][gitlab-release-cli] and links the generic package as evidence.

[gitlab-generic-package]: https://docs.gitlab.com/ee/user/packages/generic_packages/

[gitlab-nuget-repository]: https://docs.gitlab.com/ee/user/packages/nuget_repository/

[gitlab-release-cli]: https://gitlab.com/gitlab-org/release-cli/-/tree/master/docs

[gitlab-container-registry]: https://docs.gitlab.com/ee/user/packages/container_registry/

[guided-exploration-manifesto]: https://gitlab.com/guided-explorations/guided-exploration-concept/-/blob/master/README.md

[readme]: https://gitlab.com/guided-explorations/devops-patterns/utterly-automated-versioning/-/blob/develop/README.md

[utterly-automated-versioning]: https://gitlab.com/guided-explorations/devops-patterns/utterly-automated-versioning/

[gitlab-release-help]: https://docs.gitlab.com/ee/user/project/releases/
