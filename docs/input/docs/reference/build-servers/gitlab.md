---
Order: 50
Title: GitLab CI
Description: Details on the GitLab CI support in GitVersion
RedirectFrom: docs/build-server-support/build-server/gitlab
---

To use GitVersion with GitLab CI, either use the [MSBuild
Task](/docs/usage/msbuild) or put the GitVersion executable in your
runner's `PATH`.

A working example of integrating GitVersion with GitLab is maintained in the project [Utterly Automated Software and Artifact Versioning with GitVersion][]

Here is a summary of what it demonstrated (many more details in the [README.md][])

- Is a reusable working example known as a Guided Exploration [(GE Manifesto)][] - so job logs and package artifacts can be reviewed. The project can also be imported to your own GitLab group or instance as a starting point for your own work.
- IMPORTANT: It demonstrates how to override GitLab CI's default cloning behavior so that GitVersion can do a dynamic copy. Selectively clones GitVersion.yml so that these settings take effect. This best practice demonstrates the best way to do this while avoiding a double-cloning of the project (once by GitLab Runner and once by GitVersion). 
- Implements GitVersion as a CI CD Extension that can be reused across many projects using includes.
- Implements GitVersion as a single job that runs the GitVersion container and passes the version number downstream into both PIPELINE and JOB level variables, which means...
- It can be used with ANY coding language, framework or packaging engine.
- Generates example packaged artifacts:
  - Two ways of building Sem Versioned NuGet packages (msbuild-ish and nuget.exe-ish) and uploads them and tests them from a [GitLab NuGet repository][].
  - A Sem Versioned [GitLab Generic Package][]
  - A Sem Versioned docker container and uploads to [GitLabs Container Registry][].
- It creates a Sem Versioned [GitLab Release](GitLab Releases Feature Help) and Git tag using the [GitLab release-cli][] and links the generic package as evidence.

[GitLab Generic Package]: https://docs.gitlab.com/ee/user/packages/generic_packages/
[GitLab NuGet repository]: https://docs.gitlab.com/ee/user/packages/nuget_repository/
[GitLab release-cli]: https://gitlab.com/gitlab-org/release-cli/-/tree/master/docs
[GitLabs Container Registry]: https://docs.gitlab.com/ee/user/packages/container_registry/
[(GE Manifesto)]: https://gitlab.com/guided-explorations/guided-exploration-concept/-/blob/master/README.md
[README.md]: https://gitlab.com/guided-explorations/devops-patterns/utterly-automated-versioning/-/blob/develop/README.md
[Utterly Automated Software and Artifact Versioning with GitVersion]: https://gitlab.com/guided-explorations/devops-patterns/utterly-automated-versioning/
