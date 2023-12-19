namespace GitVersion.Core;

internal interface IBranchRepository
{
    IEnumerable<IBranch> GetMainlineBranches(params IBranch[] excludeBranches);

    IEnumerable<IBranch> GetReleaseBranches(params IBranch[] excludeBranches);
}
