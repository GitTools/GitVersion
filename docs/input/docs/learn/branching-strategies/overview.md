---
Order: 10
Title: Overview
Description: An overview of the different branching strategies supported by GitVersion
---

There are two mainstream branching strategies in git and many lesser known
strategies.

When building GitVersion we had to work through not only how to use the
branching strategies but how we could fully support semantic versioning in each
of these strategies.

## Introduction to branching strategies

Git is a very powerful tool and if you do not settle on a branching strategy and
associated workflows then you will likely lose work at some point. At the start
of any project I recommend picking a branching strategy and making sure your
whole team understands it.

As mentioned above the GitVersion docs cover [GitHubFlow][githubflow] and
[GitFlow][gitflow].

### GitHub Flow

GitHubFlow is a simple and powerful branching strategy. It is what GitHub uses
and the branching strategy most open source projects use.

* [Mainline development][mainline] on `main`.
* Work on [feature branches][feature-branches], merge into `main` via a [pull
    request][pull-request].
* Works well for [continuous delivery][continuous-delivery].
* Does not have a way to manage/maintain old releases.
* Only allows working on a single release at a time.

### Git Flow

GitFlow is a more complex and complete branching strategy. It also gives much
more control over when features and code is released.

* Development on `develop` branch.
* `main` only contains _released_ code.
* Supports maintaining old releases (like nServiceBus, they support the last 3
    major versions with bug fixes and security updates).
* Supports development on multiple releases at one time.

## Choosing a branching strategy

There are a few reasons you would pick GitFlow over GitHubFlow, they are:

1. You need to support multiple major versions at the same time.
2. You need to work on multiple releases at the same time.

* For example a new feature which will go in the next major version, while bug
    fixes/smaller features are still going into the current release

But if you do not have a good reason to go with GitFlow, then start with
GitHubFlow. It is a far simpler model and if you end up needing GitFlow later,
it is [easy to convert][converting-to-gitflow].

[continuous-delivery]: /docs/reference/modes/continuous-delivery
[converting-to-gitflow]: /docs/learn/branching-strategies/gitflow/converting-to-gitflow
[feature-branches]: /docs/learn/branching-strategies/gitflow/examples#feature-branches
[gitflow]: /docs/learn/branching-strategies/gitflow
[githubflow]: /docs/learn/branching-strategies/githubflow
[mainline]: /docs/reference/modes/mainline
[pull-request]: /docs/learn/branching-strategies/gitflow/examples#pull-request
