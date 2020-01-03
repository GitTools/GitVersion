---
Order: 10
Title: Versioning Modes
---

GitVersion has multiple modes to fit different different ways of working.

## Continuous Delivery

[Continuous Delivery](./continuous-delivery) is the default mode. In
this mode, GitVersion calculates the next version and will use that until that
is released.

## Continuous Deployment

Sometimes you just want the version to keep changing and deploy continuously.
In this case, [Continuous Deployment](./continuous-deployment) is a
good mode to operate GitVersion by.

## Mainline Development

[Mainline Development](./mainline-development) works more like the
[Continuous Delivery](./continuous-delivery), except that it tells
GitVersion to *infer* releases from merges and commits to `master`.
