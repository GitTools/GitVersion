using GitFlowVersion;
using LibGit2Sharp;

public class PartialStubModuleWeaver : ModuleWeaver
{
    public override VersionInformation GetSemanticVersion(Repository repo)
    {
        return new VersionInformation
               {
                   Major = 1,
                   Minor = 2,
                   Patch = 3,
                   PreReleaseNumber = 4,
                   Stability = Stability.Beta,
                   BranchType = BranchType.Develop,
                   BranchName = "develop",
                   Suffix = "1234" //eg: pull request no
               };
    }
}