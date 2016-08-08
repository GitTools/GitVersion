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

## Mainline development
[Mainline development](/reference/mainline-development) works more like the
[Continuous Delivery mode](/reference/continuous-delivery), except that
it tells GitVersion to *infer* releases from merges and commits to `master`.

This mode is great if you do not want to tag each release because you simply
deploy every commit to master. The behaviour of this mode is as follows:

1. Calclate a base version (likely a tag in this mode)
2. Walk all commits from the base version commit
3. When a merge commit is found:
    - Calculate increments for each direct commit on master
    - Calculate the increment for the branch
4. Calculate increments for each remaining direct commit
5. For feature branches then calculate increment for the commits so far on your
   feature branch.

If you *do not want* GitVersion to treat a commit or a pull request as a release
and increment the version you can use `+semver: none` or `+semver: skip` in a
commit message to skip incrementing for that commit.

Here is an example of what mainline development looks like:

![Mainline mode](./img/mainline-mode.png)

**WARNING:** This approach can slow down over time, we recommend to tag
intermitently (maybe for minor or major releases) because then GitVersion
will start the version calculation from that point. Much like a snapshot in an
event sourced system. We will probably add in warnings to tag when things are
slowing down.