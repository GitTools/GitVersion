using GitVersion.OutputVariables;

namespace GitVersion.Tests;

internal record TestableGitVersionVariables() : GitVersionVariables(
    AssemblySemFileVer: "",
    AssemblySemVer: "",
    BranchName: "",
    BuildMetaData: "",
    CommitDate: "",
    CustomVersion: "",
    EscapedBranchName: "",
    FullBuildMetaData: "",
    FullSemVer: "",
    InformationalVersion: "",
    Major: "",
    MajorMinorPatch: "",
    Minor: "",
    Patch: "",
    PreReleaseLabelName: "",
    PreReleaseLabelNameWithDash: "",
    PreReleaseNumber: "",
    PreReleaseLabel: "",
    PreReleaseLabelWithDash: "",
    SemVer: "",
    Sha: "",
    ShortSha: "",
    UncommittedChanges: "",
    VersionSourceDistance: "",
    VersionSourceIncrement: "",
    VersionSourceSemVer: "",
    VersionSourceSha: "",
    WeightedPreReleaseNumber: ""
);
