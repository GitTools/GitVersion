---
Order: 40
Title: Continua CI Setup
---

This guide explains how to run GitVersion inside [Continua CI](https://www.finalbuilder.com/continua-ci).

## Assumptions

This guide assumes a few variables are present in the configuration. Note that
this example uses `Catel` as repository name, but it should be replaced by the
name of the repository where GitVersion is running against.

* RepositoryBranchName => $Source.Catel.BranchName$
* RepositoryCommitId => $Source.Catel.LatestChangeset.Id$
* RepositoryName => Catel
* RepositoryName => $Source.Catel.Path$
* RepositoryUrl => $Source.Catel.Url$

It also requires a few variables which will automatically be filled by
GitVersion. The example below are just a few, any of the GitVersion variables
written to the output can be used.

* GitVersion_FullSemVer
* GitVersion_MajorMinorPatch
* GitVersion_NuGetVersion

You also need to add a property collector for the agents to detect the
GitVersion tool on the agents:

* Namespace => GitVersion
* Run On => Agent
* Type => Path Finder Plugin
* Property Name => Path
* Executable => GitVersion.exe
* Search paths => your installation folder (e.g. `C:\Tools\GitVersion` or if you
are using Chocolatey `C:\ProgramData\chocolatey\lib\GitVersion.Portable\tools`)

## Basic Usage

To run GitLink inside [Continua CI](https://www.finalbuilder.com/continua-ci),
follow the steps below:

* Add a new `Execute Program` step to a stage
* In the `Execute Program` tab, set the following values:
  * Executable path: $Agent.GitVersion.Path$
  * Working directory: %RepositoryPath%
* In the `Arguments` tab, set the following values:
  * Arguments: /url %RepositoryUrl% /b %RepositoryBranchName% /c %RepositoryCommitId% /output buildserver
* In the `Options` tab, set the following values:
  * Wait for completion: checked
  * Log output: checked
  * Check program exit code: checked
  * Exit code must be: equal to
  * Exit code: 0

Now GitVersion will automatically run and fill the `GitVersion_` variables.
