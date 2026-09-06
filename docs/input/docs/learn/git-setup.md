---
Order: 20
Title: Repository setup
RedirectFrom: docs/reference/git-setup
---
GitVersion needs the commit history and references used by your configuration. Remote names alone do not determine the version.

## History and references

Start with an unshallow checkout and fetch the tags and branches required by your workflow. Ensure that the configuration file is present. Follow [repository requirements](/docs/reference/requirements) and the guide for your [CI provider](/docs/reference/build-servers).

## Branch context

Build servers sometimes check out a commit in detached HEAD state. Use the provider's supported branch detection and checkout configuration rather than assuming a local branch name is available. See [environment variables](/docs/reference/environment-variables).

## Remotes

### upstream

A fork-based contributor workflow often calls the original project's remote `upstream`. This is a naming convention for contributors, not a universal GitVersion requirement.

### origin

A checkout normally has a remote called `origin`. It may refer to the main repository or a fork. Fetch the references that your versioning configuration needs; do not rename remotes simply to follow a contributor example.
