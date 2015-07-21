# GitFlow Examples
## Feature Branches
Feature branches will take the feature branch name and use that as the pre-release tag.

![GitFlow](http://uml.mvnsearch.org/gist/05119d0cd4ecaaefff94)


## Pull Request
Because feature branches are most likely pushed to a fork, we are showing the
pull request branch name which is created when you submit a pull request

![GitFlow](http://uml.mvnsearch.org/gist/09fdf46995b771f3164a)


## Hotfix Branches
Hotfix branches are used when you need to do a *patch* release in GitFlow and are always created off `master`

![GitFlow](http://uml.mvnsearch.org/gist/f26ae57adbd9b74f74c4)


## Minor Release Branches
Release branches are used for both major and minor releases for stabilisation before a release. Release branches are taken off `develop` then merged to both `develop` and `master`. Finally `master` is tagged with the released version.

![GitFlow](http://uml.mvnsearch.org/gist/6d33d35a70a777608fa1)


## Major Release Branches
Major releases are just like minor releases, the difference is you bump the major in the release branch name.

![GitFlow](http://uml.mvnsearch.org/gist/39f9d8b8b007c82f1f80)


## Support Branches
Support branches are not really covered in GitFlow, but are essential if you need to maintain multiple major versions at the same time. You could use support branches for supporting minor releases as well. If you are just supporting the majors, then name your branch `support/<major>.x` (i.e `support/1.x`), to support minors use `support/<major>.<minor>.x` or `support/<major>.<minor>.0`. (i.e `support/1.3.x` or `support/1.3.0`)

### Hotfix
Depending on what you name your support branch, you may or may not need a hotfix branch. Naming it `support/1.x` will automatically bump the patch, if you name it `support/1.3.0` then the version in branch name rule will kick in and the patch *will not* automatically bump, meaning you have to use hotfix branches.
![GitFlow](http://uml.mvnsearch.org/gist/b035b8ca99bd34239518)


### Minor Release
![GitFlow](http://uml.mvnsearch.org/gist/2167fb1c4a5cf84edfd8)


## Source

See `DocumentationSamples.GitFlowExample`. To update, modify then run test. Update [https://gist.github.com/JakeGinnivan/cf053d7f5d336ae9f7bb](https://gist.github.com/JakeGinnivan/cf053d7f5d336ae9f7bb)
