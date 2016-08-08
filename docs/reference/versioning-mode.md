# Versioning modes
GitVersion has multiple modes to fit different different ways of working.

## Continuous Delivery
[Continuous Delivery](/reference/continuous-delivery) is the default mode. In
this mode, GitVersion calculates the next version and will use that until that
is released.

## Continuous Deployment
Sometimes you just want the version to keep changing and deploy continuously.
In this case, [Continuous Deployment](/reference/continuous-deployment) is a
good mode to operate GitVersion by.

## Mainline Development
[Mainline Development](/reference/mainline-development) works more like the
[Continuous Delivery](/reference/continuous-delivery), except that it tells
GitVersion to *infer* releases from merges and commits to `master`.