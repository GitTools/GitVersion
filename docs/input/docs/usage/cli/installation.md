---
Order: 10
Title: Installation
Description: How to install the GitVersion Command Line Interface
---

GitVersion's command line interface can be installed and consumed in many
different ways. Read about the options below.

### .NET Global Tool

GitVersion can be installed as a [.NET global tool][dotnet-tool] under the name
[`GitVersion.Tool`][tool] by executing the following in a terminal:

```shell
dotnet tool install --global GitVersion.Tool --version 5.*
```

If you want to pin to a specific version of GitVersion, you can find the available
versions of [`GitVersion.Tool` on NuGet](https://www.nuget.org/packages/GitVersion.Tool/).

This should work on all operating systems supported by .NET Core.

To run call

```shell
dotnet-gitversion
```

### Homebrew

To install the [`gitversion`][brew] formula with [Homebrew][homebrew],
enter the following into a terminal:

```shell
brew install gitversion
```

Switches are available with `gitversion --help`. Even though the documentation
uses a slash `/` for all switches, you need to use a dash `-` instead, since `/`
is interpreted as a root path on POSIX based operating systems.

This should work on all operating systems supported by Homebrew (at the time
of writing: Linux and macOS).

### Chocolatey

Available on [Chocolatey](https://chocolatey.org) as
[`GitVersion.Portable`][choco].

```shell
choco install GitVersion.Portable
```

This should work on all operating systems supported by Chocolatey (at the time
of writing: Windows).

### Docker

[`gittools/gitversion`][docker] allows you to use GitVersion through Docker,
without installing any other dependencies. To use the Docker image, execute
the following:

```shell
docker run --rm -v "$(pwd):/repo" gittools/gitversion:5.6.6 /repo
```

The important arguments here are:

|                    Argument | Description                                                                                                  |
| --------------------------: | :----------------------------------------------------------------------------------------------------------- |
|            `"$(pwd):/repo"` | Maps the output of `pwd` (the working directory) to the `/repo` directory within the Docker container.       |
| `gittools/gitversion:5.6.6` | The name and tag of the GitVersion container to use.                                                         |
|                     `/repo` | The directory within the Docker container GitVersion should use as its working directory. Don't change this. |

:::{.alert .alert-warning}
**Caveat:** The `/output buildserver` option doesn't work universally with
Docker since environment variables defined inside the Docker container will not
be exposed to the host OS.
:::

This should work on all operating systems supported by Docker (at the time
of writing: Linux, macOS, Windows).

[dotnet-tool]: https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools#install-a-global-tool
[tool]: https://www.nuget.org/packages/GitVersion.Tool/
[brew]: https://formulae.brew.sh/formula/gitversion
[homebrew]: https://brew.sh/
[docker]: https://hub.docker.com/r/gittools/gitversion
[choco]: https://chocolatey.org/packages/GitVersion.Portable
