namespace GitFlowVersion
{
    public class VersionAndBranchAndDate : VersionAndBranch
    {
        public ReleaseDate ReleaseDate;

        public VersionAndBranchAndDate() { }

        public VersionAndBranchAndDate(VersionAndBranch vab, ReleaseDate rd)
        {
            Version = vab.Version;
            BranchType = vab.BranchType;
            BranchName = vab.BranchName;
            Sha = vab.Sha;
            ReleaseDate = rd;
        }
    }
}
