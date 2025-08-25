---
Order: 20
Title: Version Variables
Description: The version variables exposed by GitVersion
RedirectFrom: docs/more-info/variables
---

Version variables are quite useful if you need different formats of the version
number. Running the `gitversion` executable in your repository will show you
what is available. For the `release/3.0.0` branch of GitVersion it shows:

```json
{
    "Major": 3,
    "Minor": 22,
    "Patch": 11,
    "PreReleaseTag": "beta.99",
    "PreReleaseTagWithDash": "-beta.99",
    "PreReleaseLabel": "beta",
    "PreReleaseLabelWithDash": "-beta",
    "PreReleaseNumber": 99,
    "WeightedPreReleaseNumber": 1099,
    "BuildMetaData": 88,
    "FullBuildMetaData": "99.Branch.release/3.22.11.Sha.28c853159a46b5a87e6cc9c4f6e940c59d6bc68a",
    "MajorMinorPatch": "3.22.11",
    "SemVer": "3.22.11-beta.99",
    "AssemblySemVer": "3.22.11.0",
    "AssemblySemFileVer": "3.22.11.0",
    "InformationalVersion": "3.22.11-beta.99+88.Branch.release/3.022.011.Sha.28c853159a46b5a87e6cc9c4f6e940c59d6bc68a",
    "CustomVersion": "3.22.11-beta.99",
    "FullSemVer": "3.22.11-beta.99+88",
    "BranchName": "release/3.022.011",
    "EscapedBranchName": "release-3.022.011",
    "Sha": "28c853159a46b5a87e6cc9c4f6e940c59d6bc68a",
    "ShortSha": "28c8531",
    "VersionSourceSha": "28c853159a46b5a87e6cc9c4f6e940c59d6bc68a",
    "CommitsSinceVersionSource": 7,
    "CommitDate": "2021-12-31",
    "UncommittedChanges": 0
}
```

Each property of the above JSON document is described in the below table.

|                          Property | Description                                                                                                                                                                |
| --------------------------------: | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
|                           `Major` | The major version. Should be incremented on breaking changes.                                                                                                              |
|                           `Minor` | The minor version. Should be incremented on new features.                                                                                                                  |
|                           `Patch` | The patch version. Should be incremented on bug fixes.                                                                                                                     |
|                   `PreReleaseTag` | The pre-release tag is the pre-release label suffixed by the `PreReleaseNumber`.                                                                                           |
|           `PreReleaseTagWithDash` | The pre-release tag prefixed with a dash.                                                                                                                                  |
|                 `PreReleaseLabel` | The pre-release label.                                                                                                                                                     |
|         `PreReleaseLabelWithDash` | The pre-release label prefixed with a dash.                                                                                                                                |
|                `PreReleaseNumber` | The pre-release number.                                                                                                                                                    |
|        `WeightedPreReleaseNumber` | A summation of branch specific `pre-release-weight` and the `PreReleaseNumber`. Can be used to obtain a monotonically increasing version number across the branches.       |
|                   `BuildMetaData` | The build metadata, usually representing number of commits since the `VersionSourceSha`. Despite its name, will not increment for every build.                             |
|               `FullBuildMetaData` | The `BuildMetaData` suffixed with `BranchName` and `Sha`.                                                                                                                  |
|                 `MajorMinorPatch` | `Major`, `Minor` and `Patch` joined together, separated by `.`.                                                                                                            |
|                          `SemVer` | The semantical version number, including `PreReleaseTagWithDash` for pre-release version numbers.                                                                          |
|                  `AssemblySemVer` | Suitable for .NET `AssemblyVersion`. Defaults to `Major.Minor.0.0` to allow the assembly to be hotfixed without breaking existing applications that may be referencing it. |
|              `AssemblySemFileVer` | Suitable for .NET `AssemblyFileVersion`. Defaults to `Major.Minor.Patch.0`.                                                                                                |
|            `InformationalVersion` | Suitable for .NET `AssemblyInformationalVersion`. Defaults to `FullSemVer` suffixed by `FullBuildMetaData`.                                                                |
|                   `CustomVersion` | The custom version, suitable for non-semantic package managers with formatting changes, e.g. NuGet. Defaults to `SemVer`.                                                               |
|                      `FullSemVer` | The full, SemVer 2.0 compliant version number.                                                                                                                             |
|                      `BranchName` | The name of the checked out Git branch.                                                                                                                                    |
|               `EscapedBranchName` | Equal to `BranchName`, but with `/` replaced with `-`.                                                                                                                     |
|                             `Sha` | The SHA of the Git commit.                                                                                                                                                 |
|                        `ShortSha` | The `Sha` limited to 7 characters.                                                                                                                                         |
|                `VersionSourceSha` | The SHA of the commit used as version source.                                                                                                                              |
|       `CommitsSinceVersionSource` | The number of commits since the version source.                                                                                                                            |
|                      `CommitDate` | The ISO-8601 formatted date of the commit identified by `Sha`.                                                                                                             |
|              `UncommittedChanges` | The number of uncommitted changes present in the repository.                                                                                                               |

Depending on how and in which context GitVersion is executed (for instance
within a [supported build server][build-servers]), the above version variables
may be exposed automatically as **environment variables** in the format
`GitVersion_FullSemVer`.

## Formatting Variables

GitVersion variables can be formatted using C# format strings. See [Format Strings](/docs/reference/custom-formatting) for details.

[build-servers]: ./build-servers/
