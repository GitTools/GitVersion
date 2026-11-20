using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

internal interface IMainlineCommitResolver
{
    // Configuration inheritance follows predecessors, then the receiving merge (BranchedTo).
    IEnumerable<MainlineCommit> Traverse(MainlineCommit commit);

    // An empty iteration starts at its recorded branch point because it has no local commit.
    IEnumerable<MainlineCommit> Traverse(MainlineIteration iteration);

    MainlineCommit? Resolve(MainlineIteration iteration, ICommit? commit);
}
