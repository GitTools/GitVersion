---
Order: 5
Title: Continuous Integration
Description: |
    GitVersion can be used in a Continuous Server pipeline to generate a
    version number that both labels the build itself and makes the different
    version variables available to the rest of the build pipeline.
CardIcon: repeat.svg
---

GitVersion can be used in a Continuous Server pipeline to generate a version
number that both labels the build itself and makes the different version
variables available to the rest of the build pipeline. Choose between the
supported continuous integration servers below.

## GitHub Actions

GitVersion's GitTools Actions allows for simple integration into a GitHub
Actions build pipeline.

[GitTools Actions][gittools-actions]{.btn .btn-primary}

## Azure DevOps

GitVersion's GitTools Azure DevOps Task allows for simple integration of
GitVersion into an Azure DevOps build pipeline.

[GitTools Task][gittools-task]{.btn .btn-primary}

## GitLab CI Pipelines

The GitLab CI example [gitlab-sample][] implements GitVersion support at the pipeline level by using a single job that runs the GitVersion container and passes the version number downstream into both _pipeline_ and _job_ level variables. It is also implemented as a reusable CI/CD Extension that can be included in many different projects.

[gittools-actions]: https://github.com/marketplace/actions/gittools
[gittools-task]: https://marketplace.visualstudio.com/items?itemName=gittools.gittools
[gitlab-sample]: https://gitlab.com/guided-explorations/devops-patterns/utterly-automated-versioning/
