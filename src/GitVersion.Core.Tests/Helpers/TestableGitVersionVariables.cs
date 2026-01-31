using GitVersion.OutputVariables;

namespace GitVersion.Core.Tests.Helpers;

internal record TestableGitVersionVariables() : GitVersionVariables(
    Major: "",
    Minor: "",
    Patch: "",
    BuildMetaData: "",
    FullBuildMetaData: "",
    BranchName: "",
    EscapedBranchName: "",
    Sha: "",
    ShortSha: "",
    MajorMinorPatch: "",
    SemVer: "",
    FullSemVer: "",
    VersionSourceSemVer: "",
    AssemblySemVer: "",
    AssemblySemFileVer: "",
    PreReleaseTag: "",
    PreReleaseTagWithDash: "",
    PreReleaseLabel: "",
    PreReleaseLabelWithDash: "",
    PreReleaseNumber: "",
    WeightedPreReleaseNumber: "",
    InformationalVersion: "",
    CommitDate: "",
    VersionSourceSha: "",
    CommitsSinceVersionSource: "",
    UncommittedChanges: ""
);
