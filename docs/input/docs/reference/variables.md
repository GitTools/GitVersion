---
Order: 20
Title: Version Variables
Description: The version variables exposed by GitVersion
---

Version variables are quite useful if you need different formats of the version
number. Running the `gitversion` executable in your repository will show you
what is available. For the `release/3.0.0` branch of GitVersion it shows:

```json
{
    "Major": 3,
    "Minor": 0,
    "Patch": 0,
    "PreReleaseTag": "beta.1",
    "PreReleaseTagWithDash": "-beta.1",
    "PreReleaseLabel": "beta",
    "PreReleaseLabelWithDash": "-beta",
    "PreReleaseNumber": 1,
    "WeightedPreReleaseNumber": 1001,
    "BuildMetaData": 1,
    "BuildMetaDataPadded": "0001",
    "FullBuildMetaData": "1.Branch.release/3.0.0.Sha.28c853159a46b5a87e6cc9c4f6e940c59d6bc68a",
    "MajorMinorPatch": "3.0.0",
    "SemVer": "3.0.0-beta.1",
    "LegacySemVer": "3.0.0-beta1",
    "LegacySemVerPadded": "3.0.0-beta0001",
    "AssemblySemVer": "3.0.0.0",
    "AssemblySemFileVer": "3.0.0.0",
    "InformationalVersion": "3.0.0-beta.1+1.Branch.release/3.0.0.Sha.28c853159a46b5a87e6cc9c4f6e940c59d6bc68a",
    "FullSemVer": "3.0.0-beta.1+1",
    "BranchName": "release/3.0.0",
    "EscapedBranchName": "release-3.0.0",
    "Sha": "28c853159a46b5a87e6cc9c4f6e940c59d6bc68a",
    "ShortSha": "28c8531",
    "NuGetVersionV2": "3.0.0-beta0001",
    "NuGetVersion": "3.0.0-beta0001",
    "NuGetPreReleaseTagV2": "beta0001",
    "NuGetPreReleaseTag": "beta0001",
    "VersionSourceSha": "950d2f830f5a2af12a6779a48d20dcbb02351f25",
    "CommitsSinceVersionSource": 1,
    "CommitsSinceVersionSourcePadded": "0001",
    "CommitDate": "2014-03-06",
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
|                   `BuildMetaData` | The build metadata, usually representing number of commits since the `VersionSourceSha`.                                                                                   |
|             `BuildMetaDataPadded` | The `BuildMetaData` padded with `0` up to 4 digits.                                                                                                                        |
|               `FullBuildMetaData` | The `BuildMetaData` suffixed with `BranchName` and `Sha`.                                                                                                                  |
|                 `MajorMinorPatch` | `Major`, `Minor` and `Patch` joined together, separated by `.`.                                                                                                            |
|                          `SemVer` | The semantical version number, including `PreReleaseTagWithDash` for pre-release version numbers.                                                                          |
|                    `LegacySemVer` | Equal to `SemVer`, but without a `.` separating `PreReleaseLabel` and `PreReleaseNumber`.                                                                                  |
|              `LegacySemVerPadded` | Equal to `LegacySemVer`, but with `PreReleaseNumber` padded with `0` up to 4 digits.                                                                                       |
|                  `AssemblySemVer` | Suitable for .NET `AssemblyVersion`. Defaults to `Major.Minor.0.0` to allow the assembly to be hotfixed without breaking existing applications that may be referencing it. |
|              `AssemblySemFileVer` | Suitable for .NET `AssemblyFileVersion`. Defaults to `Major.Minor.Patch.0`.                                                                                                |
|            `InformationalVersion` | Suitable for .NET `AssemblyInformationalVersion`. Defaults to `FullSemVer` suffixed by `FullBuildMetaData`.                                                                |
|                      `FullSemVer` | The full, SemVer 2.0 compliant version number.                                                                                                                             |
|                      `BranchName` | The name of the checked out Git branch.                                                                                                                                    |
|               `EscapedBranchName` | Equal to `BranchName`, but with `/` replaced with `-`.                                                                                                                     |
|                             `Sha` | The SHA of the Git commit.                                                                                                                                                 |
|                        `ShortSha` | The `Sha` limited to 7 characters.                                                                                                                                         |
|                  `NuGetVersionV2` | A NuGet 2.0 compatible version number.                                                                                                                                     |
|                    `NuGetVersion` | A NuGet 1.0 compatible version number.                                                                                                                                     |
|            `NuGetPreReleaseTagV2` | A NuGet 2.0 compatible `PreReleaseTag`.                                                                                                                                    |
|              `NuGetPreReleaseTag` | A NuGet 1.0 compatible `PreReleaseTag`.                                                                                                                                    |
|                `VersionSourceSha` | The SHA of the commit used as version source.                                                                                                                              |
|       `CommitsSinceVersionSource` | The number of commits since the version source.                                                                                                                            |
| `CommitsSinceVersionSourcePadded` | The `CommitsSinceVersionSource` padded with `0` up to 4 digits.                                                                                                            |
|                      `CommitDate` | The ISO-8601 formatted date of the commit identified by `Sha`.                                                                                                             |
|              `UncommittedChanges` | The number of uncommitted changes present in the repository.                                                                                                               |

Depending on how and in which context GitVersion is executed (for instance
within a [supported build server][build-servers]), the above version variables
may be exposed automatically as **environment variables** in the format
`GitVersion_FullSemVer`.

[build-servers]: ./build-servers/
