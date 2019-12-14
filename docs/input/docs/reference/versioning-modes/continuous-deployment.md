---
Order: 30
Title: Continuous Deployment
---

Continuous Deployment is the process of checking into master, running all the
tests and if everything goes green it is automatically pushed to production.

A good case for Continuous Deployment is when using Octopus deploy, as you
cannot publish the same version of a package into the same feed.

For this mode we follow the logic in [this blog post by Xavier Decoster][blog]
on the issues of incrementing automatically.

As such we force a pre-release tag on all branches, this is fine for
applications but can cause problems for libraries. As such this mode may or may
not work for you, which leads us into a new mode in v4 of GitVersion:
[Mainline Development](mainline-development).

### Usage

By default GitVersion is set up to do Continuous Deployment versioning on the
`develop` branch, but for all other branches,
[Continuous Delivery](continuous-delivery) is the default mode. From version
3 of GitVersion this behavior is [configurable](../../configuration).

The default behavior for v3 and how v1 & 2 worked was that the version only
incremented after a tag, which signified a release. In v3 you can simply switch
the default mode in the [configuration](../../configuration) from
`ContinuousDelivery` to `ContinuousDeployment` and the version will then
increment each commit, giving you the features of GitVersion with continuous
deployment:

```yaml
mode: ContinuousDeployment
```

[blog]: http://www.xavierdecoster.com/semantic-versioning-auto-incremented-nuget-package-versions
