---
Order: 30
Title: Continuous Deployment
Description: |
    Sometimes you just want the version to keep changing and deploy continuously.
RedirectFrom: docs/reference/versioning-modes/continuous-deployment
---

Continuous Deployment is the process of checking into main and automatically
deploying to production.

For this mode we follow the logic in [this blog post by Xavier Decoster][blog]
on the issues of incrementing automatically.

## How Continuous Deployment affects GitVersion

The thing about continuous deployment is that there will be only one version
to deploy on production. This means that GitVersion will build
**the same semantic version** for every commit until it has been tagged. For instance:

* 1.2.0
* 1.1.0 (tag: 1.1.0) <-- This is the version which has been deployed on production
* 1.1.0
* 1.1.1

Tags are required in this mode to communicate when the deployment happens on production.

## Resources

* [Configuration][configuration]
* [Semantic Versioning & auto-incremented NuGet package versions][blog]

[configuration]: /docs/reference/configuration

[blog]: https://www.xavierdecoster.com/semantic-versioning-auto-incremented-nuget-package-versions
