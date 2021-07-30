---
Order: 50
Title: GitLab CI
Description: Details on the GitLab CI support in GitVersion
---

To use GitVersion with GitLab CI, either use the [MSBuild
Task](/docs/usage/msbuild) or put the GitVersion executable in your
runner's `PATH`.

A working example of integrating GitVersion with GitLab is maintained in the project [Utterly Automated Software and Artifact Versioning with GitVersion](https://gitlab.com/guided-explorations/devops-patterns/utterly-automated-versioning/)

Here is a summary of what it demonstrated (many more details in the [README.md](https://gitlab.com/guided-explorations/devops-patterns/utterly-automated-versioning/-/blob/develop/README.md))

- Is a working example know as a [Guided Explorations (GE Manifesto)](https://gitlab.com/guided-explorations/guided-exploration-concept/-/blob/master/README.md) - so job logs and package artifacts can be reviewed. The project can also be imported to your own GitLab group or instance.
- Implements GitVersion as a CI CD Extension that can be reused across many projects on the same GitLab instance using includes.
- Implements GitVersion as a single job that runs the GitVersion container and passes the version number downstream into both PIPLINE and JOB level variables, which means...
- It can be used with ANY coding language, framework or packaging engine.
- Generates example packaged artifacts:
  - Two ways of building Sem Versioned [NuGet packages](https://docs.gitlab.com/ee/user/packages/nuget_repository/) (msbuild-ish and nuget.exe-ish) and uploads them and tests them from a [GitLab NuGet repository](https://docs.gitlab.com/ee/user/packages/nuget_repository/).
  - A Sem Versioned [GitLab "Generic Package"](https://docs.gitlab.com/ee/user/packages/generic_packages/)
  - A Sem Versioned docker container and uploads to [GitLabs Container Registry](https://docs.gitlab.com/ee/user/packages/container_registry/).
- It creates a Sem Versioned [GitLab Release](GitLab Releases Feature Help) and Git tag using the [GitLab release-cli](https://gitlab.com/gitlab-org/release-cli/-/tree/master/docs) and links the generic package as evidence.
