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
    VersionSourceSemVer: "",
    VersionSourceSha: "",
    CommitsSinceVersionSource: "",
    VersionSourceDistance: "",
    UncommittedChanges: "");
