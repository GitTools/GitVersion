---
Order: 20
Title: Continuous Delivery
Description: |
    Continuous Delivery is the default versioning mode. In this mode,
    GitVersion calculates the next version and will use that until that is
    released.
RedirectFrom: docs/reference/versioning-modes/continuous-delivery
---

Continuous Delivery is the practice of having a deployment pipeline and is the
default mode in GitVersion. Each stage of the pipeline gets the code going
through the pipeline closer to production.

The topic itself is rather large, here we will just focus on the building and
creation of _releasable_ artifacts. This is only a part of continuous delivery
as a whole, with the hard part being the ability to measure the impacts of what
you have deployed into production.

In essence continuous delivery means:

*   Your code is automatically built and tested
*   If any of the automated tests fail, the team's #1 priority is to fix the
    build
*   If the build is green, the application can be deployed at any time
    *   Ideally the business should make that decision
    *   The same artifacts which were built and tested should be deployed
    *   That means no rebuilding everything when you are deploying

Continuous delivery does not work well with GitFlow. The reason is that you are
required to _merge_ to main to do a release, triggering a rebuild and a new
set of artifacts to go through your pipeline. Depending on how long your
pipeline is, this could be a while.

GitHubFlow is a better fit for Continuous delivery, the [mainline
development][mainline] model means that every merged feature branch will be
built as a _stable_ version and if the build/builds go green then you are free
to deploy to production at any time.

## Usage

By default, GitVersion is set up to do Continuous Delivery on all branches but
`develop`, which is set up with [Continuous Deployment][continuous-deployment].
To change the mode to Continuous Delivery, change your
[configuration][configuration] to:

```yaml
mode: ContinuousDelivery
```

## How Continuous Delivery affects GitVersion

The thing about continuous delivery is that there will be _multiple_ candidates
to deploy to production and it is a human choice to deploy. This means that
GitVersion will build **the same semantic version** until that version is
deployed. For instance:

*   1.1.0+5
*   1.1.0+6
*   1.1.0+7  <-- This is the artifact we release, tag the commit which created
    this version
*   1.1.1+0

Tags are required in this mode to communicate when the release is done as it's
an external manual process.

## Resources

*   [Continuous Delivery on Wikipedia][wikipedia]
*   [Continuous Delivery, the book][book]

[book]: https://www.amazon.com/Continuous-Delivery-Deployment-Automation-Addison-Wesley/dp/0321601912
[configuration]: /docs/reference/configuration
[continuous-deployment]: /docs/reference/modes/continuous-deployment
[mainline]: /docs/reference/modes/mainline
[wikipedia]: https://en.wikipedia.org/wiki/Continuous_delivery
