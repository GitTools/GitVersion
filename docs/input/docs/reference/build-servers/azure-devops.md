---
Order: 20
Title: Azure DevOps
Description: |
    Details on the Azure DevOps Build Pipeline support in GitVersion
RedirectFrom: docs/build-server-support/build-server/azure-devops
---

## Installation and usage

For Azure DevOps Services or Azure DevOps Server you can install the [GitTools Bundle][gittools-bundle].

:::{.alert .alert-danger}
**Important**

You must disable shallow fetch, either in the pipeline settings UI or by setting `fetchDepth: 0` in your `checkout` step;
without it, Azure DevOps Pipelines will perform a shallow clone, which will cause GitVersion to display an error message.
See [the Azure DevOps documentation][the-azure-devops-documentation] for more information.
:::

More information can be found at [gittools/actions][gittools-actions].

## Pipeline variables

When the CLI runs with `--output buildserver`, or `GitVersion.MsBuild` writes
build-server output, GitVersion v7 emits `GitVersion_<Property>` as both an
ordinary variable and a named-step output variable (`isOutput=true`). For example:

| Access from a later step | Example |
| --- | --- |
| Ordinary pipeline variable | `$(GitVersion_SemVer)` |
| Output from a step named `version` | `$(version.GitVersion_SemVer)` |
| Expression | `variables['GitVersion_SemVer']` |
| Bash environment variable | `$GITVERSION_SEMVER` |
| PowerShell environment variable | `$env:GITVERSION_SEMVER` |

Azure uppercases variable names and converts periods to underscores when
projecting them into the environment. Pipeline names and environment names are
separate contracts. Logging commands make values available to subsequent steps;
they do not update macros or environment variables in the running producer script.
See Microsoft's [variable reference][azure-variables] and [output-variable guide][azure-outputs].

The GitTools `gitversion-execute` task has its own output contract: it reads JSON
and emits names such as `semVer` and `GitVersion_SemVer`. Refer to the
[task documentation][gittools-actions] for supported GitVersion versions and task
outputs; task support for v7 is separate from this CLI change.

### Direct CLI across jobs and stages

The following example assumes GitVersion v7 is installed on the producer agent.
The producer step must have a `name`; downstream jobs and stages must depend on
the producer. Map outputs in the consumer job's `variables` block using the full
quoted `step.variable` key.

```yaml
stages:
- stage: Compile
  jobs:
  - job: Build
    steps:
    - checkout: self
      fetchDepth: 0
    - pwsh: dotnet-gitversion --output buildserver
      name: version
    - pwsh: |
        Write-Host "Ordinary: $(GitVersion_SemVer)"
        Write-Host "Step output: $(version.GitVersion_SemVer)"
  - job: Package
    dependsOn: Build
    variables:
      packageVersion: $[ dependencies.Build.outputs['version.GitVersion_SemVer'] ]
    steps:
    - pwsh: |
        Write-Host "Package version: $env:PACKAGE_VERSION"
      env:
        PACKAGE_VERSION: $(packageVersion)
- stage: Publish
  dependsOn: Compile
  jobs:
  - job: Release
    variables:
      releaseVersion: $[ stageDependencies.Compile.Build.outputs['version.GitVersion_SemVer'] ]
    steps:
    - pwsh: |
        Write-Host "Release version: $env:RELEASE_VERSION"
      env:
        RELEASE_VERSION: $(releaseVersion)
```

This example uses ordinary build jobs. Deployment and matrix jobs have additional
output-key rules; see Microsoft's [output-variable guide][azure-outputs].

### Upgrading from v6

Starting in v7, dotted pipeline names such as `GitVersion.SemVer` are no longer
emitted. Update macros, conditions, dependency output keys, reusable templates,
explicit environment mappings, and log parsers to `GitVersion_SemVer`. There are
no dotted aliases. See the [v6-to-v7 migration guide][azure-migration] for examples.

The ordinary environment key remains `GITVERSION_SEMVER`. Remove conflicting
user-defined variables: `GitVersion.SemVer` and `GitVersion_SemVer` project to
the same environment key, and GitVersion now writes the underscore pipeline name.
When a pipeline deliberately keeps both names with different values, explicitly
map the intended pipeline variable to a distinct environment key for scripts.

GitVersion's build-number interpolation continues to accept both
`$(GitVersion.SemVer)` and `$(GitVersion_SemVer)` placeholders. This input
compatibility does not restore dotted pipeline outputs. Disabling
`output.update-build-number` does not disable output-variable emission.

[gittools-bundle]: https://marketplace.visualstudio.com/items?itemName=gittools.gittools

[the-azure-devops-documentation]: https://learn.microsoft.com/en-us/azure/devops/pipelines/yaml-schema/steps-checkout?view=azure-pipelines#shallow-fetch

[gittools-actions]: https://github.com/GitTools/actions/blob/main/docs/examples/azure/gitversion/index.md

[azure-variables]: https://learn.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=azure-devops

[azure-outputs]: https://learn.microsoft.com/en-us/azure/devops/pipelines/process/set-variables-scripts?view=azure-devops

[azure-migration]: /docs/migration/v6-to-v7#azure-pipelines-variable-names
