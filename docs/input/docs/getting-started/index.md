---
Title: Getting started
Order: 10
Description: Install GitVersion and calculate your first version.
---
GitVersion reads your Git history and configuration to produce version variables. It does not create a release or tag for you.

## 1. Prepare your environment

For the v7 .NET tool, use the .NET 10 SDK. See [installation options](/docs/usage/cli/installation) for supported platforms and alternatives.

Use a repository with the history, tags, and branches needed by your workflow. In CI, start with a full checkout rather than a shallow clone. Read [repository requirements](/docs/reference/requirements).

These examples use v7 arguments. If your installed tool is v6, consult [Upgrading](/docs/migration/v6-to-v7).

## 2. Install GitVersion

```shell
dotnet tool install --global GitVersion.Tool --version "7.*"
dotnet-gitversion --version
```

This command requires a published stable v7 package. Before that release, build v7 from source; an unversioned tool install selects stable v6 and cannot run the v7 examples below.

```shell
git clone https://github.com/GitTools/GitVersion.git
dotnet publish GitVersion/src/GitVersion.App/GitVersion.App.csproj --configuration Release --output GitVersion/artifacts/local-cli
dotnet GitVersion/artifacts/local-cli/gitversion.dll --version
```

For a source build, replace `dotnet-gitversion` in the remaining examples with `dotnet /absolute/path/to/GitVersion/artifacts/local-cli/gitversion.dll`. Run it from the repository you want to version, or pass `--target-path /path/to/repository`.

Use `--version <package-version>` on the install command to select a specific published version. A [local tool manifest](/docs/usage/cli/installation#net-local-tool) can keep the team and CI on the same version.

## 3. Calculate a version

Open a terminal in an existing Git repository and run:

```shell
dotnet-gitversion
```

The default output is JSON. Your result depends on tags, branch history, configuration, and the tool version. To print just the semantic version:

```shell
dotnet-gitversion --show-variable SemVer
```

## 4. Understand the result

| Variable | Use |
| --- | --- |
| `SemVer` | Semantic version including a pre-release label when applicable. |
| `FullSemVer` | Semantic version including build metadata. |
| `MajorMinorPatch` | The three numeric version components. |
| `AssemblySemVer` / `AssemblySemFileVer` | .NET assembly version values. |
| `Sha` | The commit associated with the result. |

See the [complete variable reference](/docs/reference/variables). A commit count or pre-release number is not a count of how many times your CI job has run.

## 5. Make versioning explicit

[Choose a workflow](/docs/usage/choose-workflow), then [configure GitVersion.yml](/docs/usage/configure). Inspect the effective configuration with:

```shell
dotnet-gitversion --show-config
```

## Next steps

- [Use version output in scripts](/docs/usage/cli/output).
- [Set up CI](/docs/reference/build-servers).
- [Understand the calculation](/docs/learn/how-it-works).
- [Diagnose unexpected results](/docs/learn/faq).
