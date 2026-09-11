---
Order: 70
Title: GitHub Actions
Description: |
    Details on the GitHub Actions Workflow support in GitVersion
---

## Installation and usage

For GitHub Actions you can install the action from [GitTools Bundle](https://github.com/marketplace/actions/gittools).

:::{.alert .alert-danger}
**Important**

You must disable shallow fetch by setting `fetch-depth: 0` in your `checkout` step;
without it, GitHub Actions might perform a shallow clone, which will cause GitVersion to display an error message.
:::

More information can be found at [gittools/actions](https://github.com/GitTools/actions/blob/main/docs/examples/github/gitversion/index.md).

## Branch and tag builds

GitVersion uses the following [GitHub Actions default environment variables](https://docs.github.com/en/actions/reference/workflows-and-actions/variables#default-environment-variables) to identify the ref being built:

| Variable | How GitVersion uses it |
| --- | --- |
| `GITHUB_REF_TYPE` | Distinguishes tag builds (`tag`) from branch builds (`branch`). |
| `GITHUB_REF` | Supplies the full ref, such as `refs/heads/main`, `refs/pull/42/merge`, or `refs/tags/1.2.3`. |

For a tag build, GitVersion treats `GITHUB_REF` as the selected tag rather than a branch name. This also applies when manually running a workflow with `workflow_dispatch` and selecting a tag.

A historical tag can point to a commit that is no longer the tip of any branch. If normalization cannot attach HEAD to a local branch, it checks the selected local tag. When that tag resolves to the checked-out commit, GitVersion keeps HEAD detached and avoids an unnecessary remote lookup for pull-request refs. Existing branch selection takes precedence when a local branch identifies HEAD.

The selected tag must exist locally and resolve to HEAD for this behavior to apply. Checking out a different ref from the one reported by `GITHUB_REF` does not make an unrelated local tag identify the build. Keep `fetch-depth: 0` as described above so GitVersion has the full history available for version calculation.
