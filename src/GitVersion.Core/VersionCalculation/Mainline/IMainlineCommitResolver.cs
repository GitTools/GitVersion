using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

internal interface IMainlineCommitResolver
{
    MainlineCommit? Resolve(MainlineIteration iteration, ICommit? commit);
}
