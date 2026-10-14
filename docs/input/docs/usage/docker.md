---
Order: 50
Title: Docker
Description: |
    Use GitVersion through one of its many published Docker containers.
---

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

### Non-root containers and repository permissions

Images default to UID/GID `10001:10001`, with a writable home at `/home/gittools`.
The usual command needs no extra flags when that user can write to the mounted repository.
GitVersion can write repository metadata and cache files, so bind mounts must be
writable by the selected user. On Linux, use the non-root checkout owner's identity:

```sh
docker run --rm --user "$(id -u):$(id -g)" \
  -v "$(pwd):/repo" gittools/gitversion:{tag} /repo
```

The image automatically creates a private temporary home if the selected user
cannot write to the configured home. No home environment flags are needed, and
the temporary home stays inside the disposable container. Do not run this command
from a root shell, or make the host repository world-writable to bypass permissions.
For Docker Desktop on Windows/macOS, ensure the shared directory is writable by
the image's default user using the host's file-sharing permissions.

Derived images can switch to root during installation, but must restore
`USER 10001:10001` for runtime. The tool remains installed at `/tools/dotnet-gitversion`.

### Tags

Most of the tags we provide have both arm64 and amd64 variants. If you need to pull a architecture specific tag you can do that like:

```sh
docker run --rm -v "$(pwd):/repo" gittools/gitversion:{tag}-amd64 /repo
docker run --rm -v "$(pwd):/repo" gittools/gitversion:{tag}-arm64 /repo
```

The list of available containers can be found on [Docker Hub][docker-hub].

[Explore GitVersion on Docker Hub][docker-hub]{.btn .btn-primary}

[docker-hub]: https://hub.docker.com/r/gittools/gitversion
