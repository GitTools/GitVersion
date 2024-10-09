using Cake.Http;
using Cake.Json;
using Common.Utilities;

namespace Docker.Tasks;

[TaskName(nameof(DockerHubReadmePublish))]
[IsDependentOn(typeof(DockerHubReadmePublishInternal))]
[TaskDescription("Publish the DockerHub updated README.md")]
public class DockerHubReadmePublish : FrostingTask<BuildContext>;

[TaskName(nameof(DockerHubReadmePublishInternal))]
[TaskDescription("Publish the DockerHub updated README.md")]
public class DockerHubReadmePublishInternal : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = false;
        if (context.DockerRegistry == DockerRegistry.DockerHub)
        {
            shouldRun &= context.ShouldRun(context.IsStableRelease, $"{nameof(DockerHubReadmePublish)} works only for tagged releases.");
        }

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context.Credentials?.DockerHub);
        var readme = GetReadmeContent(context);

        var response = context.HttpPost("https://hub.docker.com/v2/users/login", settings =>
        {
            var credentials = context.Credentials.DockerHub;
            settings
                .SetContentType("application/json")
                .SetJsonRequestBody(new { username = credentials.Username, password = credentials.Password });
        });


        context.HttpPatch("https://hub.docker.com/v2/repositories/gittools/gitversion", settings =>
        {
            var token = context.ParseJson(response).Value<string>("token");
            settings
                .SetContentType("application/json")
                .SetAuthorization("JWT", token)
                .SetJsonRequestBody(new { full_description = readme });
        });
    }

    private static string GetReadmeContent(BuildContextBase context)
    {
        ArgumentNullException.ThrowIfNull(context.Version);
        var version = context.Version.GitVersion.MajorMinorPatch;
        const string distro = Constants.AlpineLatest;
        const string dotnetVersion = Constants.VersionLatest;
        var tag = $"{version}-{distro}-{dotnetVersion}";
        // language=markdown
        var readme = $"""
# GitVersion

![GitVersion – From git log to SemVer in no time][banner]

Versioning when using Git, solved. GitVersion looks at your git history and works out the [Semantic Version][semver] of the commit being built.

This repository contains the Docker images for [GitVersion][website]. Source code can be found at [src](https://github.com/GitTools/GitVersion)

## Usage

The recommended image to run is `alpine`, as they are the smallest Docker images we provide. This will execute GitVersion for the current working directory (`$(pwd)`) on Linux and Unix or powershell on Windows:

```sh
docker run --rm -v "$(pwd):/repo" gittools/gitversion:{tag} /repo
```

The following command will execute GitVersion for the current working directory (`%CD%`) on Windows with CMD:

```sh
docker run --rm -v "%CD%:/repo" gittools/gitversion:{tag} /repo
```

Note that the path `/repo` needs to be passed as an argument since the `gitversion` executable within the container is not aware of the fact that it's running inside a container.

### CI Agents

If you are running GitVersion on a CI agent, you may need to specify environment variables to allow GitVersion to work correctly.
For example, on Azure DevOps you may need to set the following environment variables:

```sh
docker run --rm -v "$(pwd):/repo" --env TF_BUILD=true --env BUILD_SOURCEBRANCH=$(Build.SourceBranch) gittools/gitversion:{tag} /repo
```

On GitHub Actions, you may need to set the following environment variables:

```sh
docker run --rm -v "$(pwd):/repo" --env GITHUB_ACTIONS=true --env GITHUB_REF=$(GITHUB_REF) gittools/gitversion:{tag} /repo
```

### Tags

Most of the tags we provide have both arm64 and amd64 variants. If you need to pull a architecture specific tag you can do that like:

```sh
docker run --rm -v "$(pwd):/repo" gittools/gitversion:{tag}-amd64 /repo
docker run --rm -v "$(pwd):/repo" gittools/gitversion:{tag}-arm64 /repo
```

## Quick Links

* [Documentation][docs]
* [Contributing][contribute]
* [Why GitVersion][why]
* [Usage][usage]
* [How it works][how]
* [FAQ][faq]
* [Who is using GitVersion][who]

[website]:    https://gitversion.net
[docs]:       https://gitversion.net/docs/
[contribute]: https://github.com/GitTools/GitVersion/blob/main/CONTRIBUTING.md
[why]:        https://gitversion.net/docs/learn/why
[usage]:      https://gitversion.net/docs/usage
[how]:        https://gitversion.net/docs/learn/how-it-works
[faq]:        https://gitversion.net/docs/learn/faq
[who]:        https://gitversion.net/docs/learn/who
[src]:        https://github.com/GitTools/GitVersion
[semver]:     https://semver.org
[banner]:     https://raw.githubusercontent.com/GitTools/graphics/master/GitVersion/banner-1280x640.png
""";
        return readme;
    }
}
