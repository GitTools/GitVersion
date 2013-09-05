using GitFlowVersion;
using LibGit2Sharp;

public class PartialStubModuleWeaver : ModuleWeaver
{
    public override SemanticVersion GetSemanticVersion(Repository repo)
    {
        return new SemanticVersion
               {
                   Major = 1,
                   Minor = 2,
                   Patch = 3,
                   PreRelease = 4,
                   Stage = Stage.Beta,
                   Suffix = "1234" //eg: pull request no
               };
    }
}