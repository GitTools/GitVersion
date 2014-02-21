namespace GitFlowVersion
{
    using LibGit2Sharp;

    public class VersionTaggedCommit
    {
        public Commit Commit;
        public SemanticVersion SemVer;

        public VersionTaggedCommit(Commit commit, SemanticVersion semVer)
        {
            Commit = commit;
            SemVer = semVer;
        }
    }
}