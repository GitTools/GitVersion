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
dotnet tool install --global GitVersion.Tool
```

:::{.alert .alert-info}
**Hint:** To install an older version of GitVersion.Tool, use the --version flag of dotnet tool install

Example: `dotnet tool install GitVersion.Tool --global --version 6.*`
:::

If you want to pin to a specific version of GitVersion, you can find the available
versions of [`GitVersion.Tool` on NuGet](https://www.nuget.org/packages/GitVersion.Tool/).

This should work on all operating systems supported by .NET Core.

To run call

```shell
dotnet-gitversion
```

### .NET Local Tool

GitVersion can also be installed as a [.NET local tool][dotnet-local-tool] using
a tool manifest. This approach is useful for ensuring that all team members use
the same version of GitVersion in a project.

To install GitVersion as a local tool, execute the following in your project's
root directory:

```shell
dotnet new tool-manifest # if you don't already have a manifest file
dotnet tool install GitVersion.Tool
```

This creates or updates a `.config/dotnet-tools.json` file in your repository
that specifies the version of GitVersion to use. You can commit this file to
source control.

:::{.alert .alert-info}
**Hint:** To install a specific version of GitVersion.Tool as a local tool, use the --version flag

Example: `dotnet tool install GitVersion.Tool --version 6.*`
:::

To restore tools specified in the manifest (for example, after cloning the
repository):

```shell
dotnet tool restore
```

To run the local tool, you have several options:

```shell
dotnet gitversion
```

Alternatively, you can use [`dotnet tool exec`][dotnet-tool-exec] (or its
shorthand aliases `dotnet dnx` or just `dnx`) to explicitly execute the tool:

```shell
dotnet tool exec GitVersion.Tool
# or using the shorthand alias
dotnet dnx GitVersion.Tool
# or even shorter
dnx GitVersion.Tool
```

:::{.alert .alert-info}
**Note:** The `dotnet tool exec`, `dotnet dnx`, and `dnx` commands are useful in scripts or CI/CD
pipelines where you want to be explicit about executing a local tool from the manifest.
When using these commands, you must specify the package name `GitVersion.Tool` rather than the command name.
:::

Note that local tools use `dotnet gitversion` (without the hyphen), while the
global tool uses `dotnet-gitversion` (with a hyphen).

This should work on all operating systems supported by .NET Core.

### Homebrew

To install the [`gitversion`][brew] formula with [Homebrew][homebrew],
enter the following into a terminal:

```shell
brew install gitversion
```

Switches are available with `gitversion -h`. Even though the documentation
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
docker run --rm -v "$(pwd):/repo" gittools/gitversion:latest-debian.12 /repo
```

The important arguments here are:

|                    Argument | Description                                                                                                  |
|----------------------------:|:-------------------------------------------------------------------------------------------------------------|
|            `"$(pwd):/repo"` | Maps the output of `pwd` (the working directory) to the `/repo` directory within the Docker container.       |
| `gittools/gitversion:{tag}` | The name and tag of the GitVersion container to use.                                                         |
|                     `/repo` | The directory within the Docker container GitVersion should use as its working directory. Don't change this. |

:::{.alert .alert-warning}
**Caveat:** The `/output buildserver` option doesn't work universally with
Docker since environment variables defined inside the Docker container will not
be exposed to the host OS.
:::

This should work on all operating systems supported by Docker (at the time
of writing: Linux, macOS, Windows).

[dotnet-tool]: https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools#install-a-global-tool

[dotnet-local-tool]: https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use

[dotnet-tool-exec]: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-exec

[tool]: https://www.nuget.org/packages/GitVersion.Tool/

[brew]: https://formulae.brew.sh/formula/gitversion

[homebrew]: https://brew.sh/

[docker]: https://hub.docker.com/r/gittools/gitversion

[choco]: https://chocolatey.org/packages/GitVersion.Portable
