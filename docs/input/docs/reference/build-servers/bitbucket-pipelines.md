---
Order: 35
Title: BitBucket Pipelines
Description: Details on the Atlassian BitBucket Pipelines support in GitVersion
---

## Basic Usage

To use GitVersion with Atlassian BitBucket Pipelines, you will need to install and run the GitVersion CLI tool
in your build step.

## Executing GitVersion

### Using the GitVersion CLI tool

An example pipeline is shown below:

```yml
image: mcr.microsoft.com/dotnet/sdk:6.0

clone:
  depth: full

pipelines:
  default:
    - step:
      name: Version and build
      script:
        - export PATH="$PATH:/root/.dotnet/tools"
        - dotnet tool install --global GitVersion.Tool --version 5.*
        - dotnet-gitversion /buildserver
        - source gitversion.properties
        - echo Building with semver $GITVERSION_FULLSEMVER
        - dotnet build
```

:::{.alert .alert-danger}
**Important**

You must set the `clone:depth` setting as shown above; without it, BitBucket Pipelines will perform a shallow clone, which will
cause GitVersion to display an error message.
:::

When the action `dotnet-gitversion /buildserver` is executed, it will detect that it is running in BitBucket Pipelines by the presence of
the `BITBUCKET_WORKSPACE` environment variable, which is set by the BitBucket Pipelines engine. It will generate a text file named `gitversion.properties`
which contains all the output of the GitVersion tool, exported as individual environment variables prefixed with `GITVERSION_`.
These environment variables can then be imported back into the build step using the `source gitversion.properties` action.

If you want to share the text file across multiple build steps, then you will need to save it as an artifact. A more complex example pipeline
is shown below:

```yml
image: mcr.microsoft.com/dotnet/sdk:6.0

clone:
  depth: full

pipelines:
  default:
    - step:
      name: Version
      script:
        - export PATH="$PATH:/root/.dotnet/tools"
        - dotnet tool install --global GitVersion.Tool --version 5.*
        - dotnet-gitversion /buildserver
      artifacts:
        - gitversion.properties
    - step:
      name: Build
      script:
        - source gitversion.properties
        - echo Building with semver $GITVERSION_FULLSEMVER
        - dotnet build
```

[Variables and Secrets](https://support.atlassian.com/bitbucket-cloud/docs/variables-and-secrets/)
[Clone Options](https://bitbucket.org/blog/support-for-more-clone-options-at-the-step-level)