---
Order: 20
Title: Version Variables
Description: The version variables exposed by GitVersion
RedirectFrom: docs/more-info/variables
---

Version variables are quite useful if you need different formats of the version
number. Running the `gitversion` executable in your repository will show you
what is available. The following is illustrative output; your values depend on
the repository history and effective configuration:

```json
{
    "AssemblySemFileVer": "3.22.11.0",
    "AssemblySemVer": "3.22.11.0",
    "BranchName": "release/3.022.011",
    "BuildMetaData": 88,
    "CommitCountSourceDistance": 7,
    "CommitCountSourceSha": "previous-source-commit-sha",
    "CommitDate": "2021-12-31",
    "CustomVersion": "3.22.11-beta.99",
    "EscapedBranchName": "release-3.022.011",
    "FullBuildMetaData": "99.Branch.release/3.22.11.Sha.28c853159a46b5a87e6cc9c4f6e940c59d6bc68a",
    "FullSemVer": "3.22.11-beta.99+88",
    "InformationalVersion": "3.22.11-beta.99+88.Branch.release/3.022.011.Sha.28c853159a46b5a87e6cc9c4f6e940c59d6bc68a",
    "Major": 3,
    "MajorMinorPatch": "3.22.11",
    "Minor": 22,
    "Patch": 11,
    "PreReleaseLabelName": "beta",
    "PreReleaseLabelNameWithDash": "-beta",
    "PreReleaseNumber": 99,
    "PreReleaseLabel": "beta.99",
    "PreReleaseLabelWithDash": "-beta.99",
    "SemVer": "3.22.11-beta.99",
    "SemVerSourceIncrement": "None",
    "SemVerSourceSemVer": "3.22.11",
    "SemVerSourceSha": null,
    "Sha": "28c853159a46b5a87e6cc9c4f6e940c59d6bc68a",
    "ShortSha": "28c8531",
    "UncommittedChanges": 0,
    "VersionSourceDistance": 7,
    "VersionSourceIncrement": "Minor",
    "VersionSourceSemVer": "3.22.11",
    "VersionSourceSha": "previous-source-commit-sha",
    "WeightedPreReleaseNumber": 1099
}
```

Each property of the above JSON document is described in the below table.

|                    Property | Description                                                                                                                                                                |
|----------------------------:|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
|        `AssemblySemFileVer` | Suitable for .NET `AssemblyFileVersion`. Defaults to `Major.Minor.Patch.0`.                                                                                                |
|            `AssemblySemVer` | Suitable for .NET `AssemblyVersion`. Defaults to `Major.Minor.0.0` to allow the assembly to be hotfixed without breaking existing applications that may be referencing it. |
|                `BranchName` | The name of the checked out Git branch.                                                                                                                                    |
|             `BuildMetaData` | The build metadata, usually representing number of commits since the `CommitCountSourceSha`. Despite its name, will not increment for every build.                             |
| `CommitCountSourceDistance` | Number of non-ignored commits reachable from HEAD but not from the counting anchor. A null anchor counts all reachable history. |
| `CommitCountSourceSha` | SHA of the counting anchor, or JSON null when counting all reachable history. |
|                `CommitDate` | The ISO-8601 formatted date of the commit identified by `Sha`.                                                                                                             |
|             `CustomVersion` | A custom version configured with `custom-version-format`. Empty when no format is configured.                                                                              |
|         `EscapedBranchName` | Equal to `BranchName`, but with `/` replaced with `-`.                                                                                                                     |
|         `FullBuildMetaData` | The `BuildMetaData` suffixed with `BranchName` and `Sha`.                                                                                                                  |
|                `FullSemVer` | The full, SemVer 2.0 compliant version number.                                                                                                                             |
|      `InformationalVersion` | Suitable for .NET `AssemblyInformationalVersion`. Defaults to `FullSemVer` suffixed by `FullBuildMetaData`.                                                                |
|                     `Major` | The major version. Should be incremented on breaking changes.                                                                                                              |
|           `MajorMinorPatch` | `Major`, `Minor` and `Patch` joined together, separated by `.`.                                                                                                            |
|                     `Minor` | The minor version. Should be incremented on new features.                                                                                                                  |
|                     `Patch` | The patch version. Should be incremented on bug fixes.                                                                                                                     |
|       `PreReleaseLabelName` | The name of the pre-release label, without the `PreReleaseNumber`.                                                                                                         |
| `PreReleaseLabelNameWithDash` | The pre-release label name prefixed with a dash.                                                                                                                         |
|          `PreReleaseNumber` | The pre-release number.                                                                                                                                                    |
|           `PreReleaseLabel` | The full pre-release label, including the `PreReleaseNumber` when present.                                                                                                  |
|   `PreReleaseLabelWithDash` | The pre-release label prefixed with a dash.                                                                                                                                |
|                    `SemVer` | The semantic version number, including `PreReleaseLabelWithDash` for pre-release version numbers.                                                                          |
| `SemVerSourceIncrement` | Final increment relative to the semantic baseline: `None`, `Patch`, `Minor`, or `Major`. |
| `SemVerSourceSemVer` | Baseline version supplied by the selected artifact or derivation. Mainline may use a folded baseline rather than a literal tag value. |
| `SemVerSourceSha` | SHA associated with the semantic source; JSON null for external sources such as configuration or a branch name. |
|                       `Sha` | The SHA of the Git commit.                                                                                                                                                 |
|                  `ShortSha` | The `Sha` limited to 7 characters.                                                                                                                                         |
|        `UncommittedChanges` | The number of uncommitted changes present in the repository.                                                                                                               |
|     `VersionSourceDistance` | Compatibility alias for `CommitCountSourceDistance`.                                                                                                                            |
|    `VersionSourceIncrement` | Legacy increment metadata, preserved for compatibility. Prefer `SemVerSourceIncrement`; the legacy field can be `None` even when an increment was applied.                                                               |
|       `VersionSourceSemVer` | Legacy semantic baseline. It need not correspond to `VersionSourceSha`; prefer `SemVerSourceSemVer`.                                                                                                                 |
|          `VersionSourceSha` | Legacy counting-anchor SHA. Prefer `CommitCountSourceSha`, which represents an absent anchor as null.                                                                                                                              |
|  `WeightedPreReleaseNumber` | A summation of branch specific `pre-release-weight` and the `PreReleaseNumber`. Can be used to obtain a monotonically increasing version number across the branches.       |

Depending on how and in which context GitVersion is executed (for instance
within a [supported build server][build-servers]), the above version variables
may be exposed automatically as **environment variables** in the format
`GitVersion_FullSemVer`.

## Semantic and counting sources

The semantic source and counting anchor can differ. With a `1.0.0` tag followed by one commit and `calculation.next-version: 5.0.0`, the semantic baseline is `5.0.0` and `SemVerSourceSha` is null, while `CommitCountSourceSha` points to the `1.0.0` tag and `CommitCountSourceDistance` is 1. A `release/5.0.0` branch name can supply the same external baseline.

The distance remains available when a deployment mode incorporates the count into the pre-release number and clears `BuildMetaData`. On a selected tag at HEAD, both source SHAs are HEAD and distance is zero. `Sha` and `ShortSha` always identify the current commit.

See [calculation strategies](/docs/reference/version-sources) for selection and counting rules. Missing source values are JSON null in the new fields; textual outputs such as environment variables and MSBuild properties use empty strings.

## Formatting Variables

GitVersion variables can be formatted using C# format strings. See [Format Strings](/docs/reference/custom-formatting) for details.

[build-servers]: ./build-servers/
