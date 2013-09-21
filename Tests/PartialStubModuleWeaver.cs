using GitFlowVersion;
using LibGit2Sharp;

public class PartialStubModuleWeaver : ModuleWeaver
{
    public override VersionAndBranch GetSemanticVersion(Repository repo)
    {
        return new VersionAndBranch
               {
                   BranchType = BranchType.Develop,
                   BranchName = "develop",
                   Version = new SemanticVersion
                                        {
                                            Major = 1,
                                            Minor = 2,
                                            Patch = 3,
                                            PreReleaseNumber = 4,
                                            Stability = Stability.Beta,
                                            Suffix = "1234" //eg: pull request no
                                        }
               };
    }
}