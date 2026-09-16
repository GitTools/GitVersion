---
Order: 30
Title: Requirements
Description: Requirements for successful operation of GitVersion
---

GitVersion has a few requirements that needs to be met in order to be able to
produce a version number. These requirements are enumerated below.

## Repository

The local (checked out) repository, either on a developer computer or on the
build server, needs to adhere to the below requirements.

### Unshallow

The repository should be an [unshallow][git-unshallow] clone. This means
that the `fetch-depth` in GitHub Actions should set to `0`, unless
the `--allow-shallow` flag is used. That flag does not restore missing history
or guarantee that the result matches a full clone.
Check with your [build server][build-servers] to see how it can be configured
appropriately.

### Main branch

The repository needs to have an existing local `master` or `main` branch.

### Develop branch

For some branch strategies (such as [Git Flow][gitflow]), a local `develop`
branch needs to exist.

### Configuration

If using a `GitVersion.yml` [configuration][configuration] file, that file
should be checked out otherwise it won't be found by GitVersion and default
config will apply.

## Environment

### Git Branch

When branch context is ambiguous, set `GIT_BRANCH` (or its `Git_Branch` alias) to
the branch name to use at the checked-out commit. The branch need not exist as a
ref. This does not select a tag or move HEAD. See [environment variables](/docs/reference/environment-variables)
for precedence and validation rules.

[git-unshallow]: https://git-scm.com/docs/git-fetch#Documentation/git-fetch.txt---unshallow

[gitflow]: /docs/learn/branching-strategies/gitflow

[build-servers]: /docs/reference/build-servers

[configuration]: /docs/reference/configuration
