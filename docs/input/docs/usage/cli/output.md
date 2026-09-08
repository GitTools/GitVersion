---
Order: 30
Title: Use version output
Description: Read version variables in scripts and pass them to builds.
---
By default GitVersion writes a JSON object containing its [version variables](/docs/reference/variables).

## JSON and a single variable

```shell
dotnet-gitversion
dotnet-gitversion --show-variable SemVer
```

Use `SemVer` when you need the semantic version, or select another variable for the target consuming it. See [formatting syntax](/docs/reference/custom-formatting) for custom formats.

## Write a JSON file

```shell
dotnet-gitversion --output file --output-file gitversion.json
```

Pass this file to later steps or jobs using your CI provider's artifact mechanism.

## Build-server output

```shell
dotnet-gitversion --output buildserver
```

On a supported build server, this uses the provider's variable and build-number mechanisms. Follow the [CI integration guide](/docs/reference/build-servers) for exact names and job/step scope.

## Dotenv output

```shell
dotnet-gitversion --output dotenv > gitversion.env
```

Load the file using your runner's or application's dotenv support. Writing a file does not automatically export variables into the invoking shell or later jobs.

## .NET assemblies

Use the [MSBuild task](/docs/usage/msbuild) or [assembly patching](/docs/usage/cli/assembly-patch) to put version values into build artifacts. Select the assembly variables or formatting rules appropriate for those artifacts.
