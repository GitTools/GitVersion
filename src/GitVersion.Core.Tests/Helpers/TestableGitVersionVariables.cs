using GitVersion.OutputVariables;

namespace GitVersion.Core.Tests.Helpers;

internal record TestableGitVersionVariables() : GitVersionVariables(
    AssemblySemFileVer: "",
    AssemblySemVer: "",
    BranchName: "",
    BuildMetaData: "",
    CommitDate: "",
    CommitsSinceVersionSource: "",
    EscapedBranchName: "",
    FullBuildMetaData: "",
    FullSemVer: "",
    InformationalVersion: "",
    Major: "",
    MajorMinorPatch: "",
    Minor: "",
    Patch: "",
    PreReleaseLabel: "",
    PreReleaseLabelWithDash: "",
    PreReleaseNumber: "",
    PreReleaseTag: "",
    PreReleaseTagWithDash: "",
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
