namespace GitVersion
{
    public class VersionAndBranch
    {
        public SemanticVersion Version;
        public BranchType? BranchType;
        public string BranchName;
        public string Sha;
    }
}