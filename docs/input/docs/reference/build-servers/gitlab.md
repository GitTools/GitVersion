---
Order: 50
Title: GitLab CI
Description: Details on the GitLab CI support in GitVersion
---

To use GitVersion with GitLab CI, either use the [MSBuild
Task](/docs/usage/msbuild) or put the GitVersion executable in your
runner's `PATH`.

A working example of integrating GitVersion with GitLab is maintained in the project [Utterly Automated Software and Artifact Versioning with GitVersion][1]

Here is a summary of what it demonstrated (many more details in the [README.md][2])

- Is a working example know as a [Guided Explorations (GE Manifesto)][3] - so job logs and package artifacts can be reviewed. The project can also be imported to your own GitLab group or instance.
- IMPORTANT: It demonstrates how to override GitLab CI's default cloning behavior so that GitVersion can do a dynamic copy. This best practice demonstrates the best way to do this while avoiding a double-cloning of the project (once by GitLab Runner and once by GitVersion)
- Implements GitVersion as a CI CD Extension that can be reused across many projects on the same GitLab instance using includes.
- Implements GitVersion as a single job that runs the GitVersion container and passes the version number downstream into both PIPLINE and JOB level variables, which means...
- It can be used with ANY coding language, framework or packaging engine.
- Generates example packaged artifacts:
  - Two ways of building Sem Versioned [NuGet packages][4] (msbuild-ish and nuget.exe-ish) and uploads them and tests them from a [GitLab NuGet repository][4].
  - A Sem Versioned [GitLab "Generic Package"][5]
  - A Sem Versioned docker container and uploads to [GitLabs Container Registry][6].
- It creates a Sem Versioned [GitLab Release](GitLab Releases Feature Help) and Git tag using the [GitLab release-cli][7] and links the generic package as evidence.

### References:
[1]: https://gitlab.com/guided-explorations/devops-patterns/utterly-automated-versioning/
[2]: https://gitlab.com/guided-explorations/devops-patterns/utterly-automated-versioning/-/blob/develop/README.md
[3]: https://gitlab.com/guided-explorations/guided-exploration-concept/-/blob/master/README.md
[4]: https://docs.gitlab.com/ee/user/packages/nuget_repository/
[5]: https://docs.gitlab.com/ee/user/packages/generic_packages/
[6]: https://docs.gitlab.com/ee/user/packages/container_registry/
[7]: https://gitlab.com/gitlab-org/release-cli/-/tree/master/docs