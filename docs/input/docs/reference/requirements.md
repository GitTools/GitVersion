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

The repository needs to be an [unshallow][git-unshallow] clone. This means
that the `fetch-depth` in GitHub Actions needs to be set to `0`, for instance.
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

If it is ambigous which reference (branch or tag) is being built, which is often
the case on build servers, the `Git_Branch` environment variable needs to be
defined and set to the reference being built.

[git-unshallow]: https://git-scm.com/docs/git-fetch#Documentation/git-fetch.txt---unshallow

[gitflow]: /docs/learn/branching-strategies/gitflow

[build-servers]: /docs/reference/build-servers

[configuration]: /docs/reference/configuration
