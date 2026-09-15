---
Title: .NET 11 support draft
Description: RC validation and remaining release checks for additive .NET 11 support.
---

This draft implements the additive targeting portion of
[#4998](https://github.com/GitTools/GitVersion/issues/4998). It retains .NET 10
and adds .NET 11 to the stable CLI, core, MSBuild package and tests. It does not
claim completed GA validation.

The repository uses SDK `11.0.100-rc.1.26425.128`. Build tooling and the experimental
`new-cli` tree still target .NET 10; the CLI generator retains `netstandard2.0`.
Install a .NET 10 runtime alongside the SDK to run those projects and the retained
.NET 10 tests. The development container and CI install both.

The main test matrix covers both target frameworks and both Git backends.
MSBuild packages include the published CLI and task assembly for each framework.
Standalone self-contained archives retain the .NET 10 LTS runtime.

Both task targets retain the existing Microsoft.Build 18.9.6 package references.
The available RC MSBuild package introduces a NuGet.Frameworks assembly conflict
in the test dependency graph. Building with SDK 11 does not require retargeting
those package references; re-evaluate them when compatible packages are available.

## Draft validation

On macOS ARM64, the dual-target solution, build tooling and experimental CLI
build with SDK 11 RC1. Formatting and changed workflow syntax checks pass.
Local macOS ARM64 tool packages install with SDK 10 and 11, and packaged MSBuild
tasks build net10 and net11 consumers with the expected version output. The
development Dockerfile also builds on Linux ARM64 with both runtimes installed.

The managed-backend runs pass 37,577 tests per target framework. Test validation
excludes live GitHub clone tests, performance thresholds and
one host-sensitive Git HTTP error-message test. These exclusions and package
smokes do not establish full cross-platform support. All-RID package validation
remains open after NuGet runtime downloads repeatedly timed out; the shipped RID
list has not been narrowed.

## Docker matrix

[GitTools/build-images #104](https://github.com/GitTools/build-images/pull/104)
published the .NET 10 and .NET 11 SDK/runtime base images for amd64 and arm64.
Docker images and container-based artifact validation now use the shared
`Constants.DotnetVersions` matrix for both frameworks.

The distro matrix is Alpine 3.23, CentOS Stream 10, Debian 13, Fedora 44, and
Ubuntu 26.04/24.04 LTS. GitVersion image validation against these published base
images remains part of CI; base-image publication alone does not validate the
GitVersion packages or their runtime behavior.

## Before stable release

- Recheck Microsoft's [release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/11.0/releases.json),
  RC support window and [breaking changes](https://learn.microsoft.com/dotnet/core/compatibility/11).
- Replace the RC SDK pin with a verified GA version, review MSBuild dependencies, and repeat
  build, test, format and exact-package installation checks.
- Validate local/global RID tool installation with SDK 10 and 11, and execution
  with only runtime 10, only runtime 11, and both present.
- Validate Core MSBuild task loading and generated version output for net10,
  net11 and older consumer TFMs; verify supported Windows task-host combinations.
- Validate both Git backends on each supported OS/RID, including musl and ARM64.
- Recheck .NET 11 CPU requirements: x86-64-v2 and Windows ARM64 LSE. Do not infer
  compatibility solely from an OS or RID name.
- Coordinate v7 support with GitTools Actions, independently of runtime support.

AOT/trimming, LibGit2Sharp removal and optional .NET 11 API optimizations remain
separate work. If GitVersion 7.0 ships before .NET 11 GA, revisit #4998's milestone
instead of treating this RC draft as final support.
