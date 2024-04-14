namespace GitVersion.Core;

[Flags]
internal enum TaggedSemanticVersions
{
    None = 0,

    OfBranch = 1,

    OfMergeTargets = 2,

    OfMainBranches = 4,

    OfReleaseBranches = 8,

    All = OfBranch | OfMergeTargets | OfMainBranches | OfReleaseBranches
}
