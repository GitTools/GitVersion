---
Order: 20
Title: Continuous Delivery
Description: |
    Sometimes you just want the version to keep changing and deploy continuously
	to an testing system. In this case, Continuous Delivery is a good mode to
	operate GitVersion by.
RedirectFrom: docs/reference/versioning-modes/continuous-delivery
---

Continuous Delivery is the process of checking into a branch, running all the
tests and if everything goes green it is automatically pushed to a testing system.

A good case for Continuous Delivery is when using Octopus deploy, as you
cannot publish the same version of a package into the same feed.

For this mode we follow the logic in [this blog post by Xavier Decoster][blog]
on the issues of incrementing automatically.

## How Continuous Delivery affects GitVersion

Continuous delivery is good when you deploy continuously to an testing system.

* 1.1.0-3
* 1.1.0-2 (tag: 1.1.0-2) <-- This is the version which has been deployed on testing
* 1.1.0-1
* 1.1.1-0

Tags are not required but optional in this mode to communicate when the release
is done as it's an automated process.

[configuration]: /docs/reference/configuration
[blog]: https://www.xavierdecoster.com/semantic-versioning-auto-incremented-nuget-package-versions
[wikipedia]: https://en.wikipedia.org/wiki/Continuous_delivery
