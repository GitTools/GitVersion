namespace GitVersion
{
    using LibGit2Sharp;

    public class VersionTaggedCommit
    {
        public string Tag;
        public Commit Commit;
        public SemanticVersion SemVer;

        public VersionTaggedCommit(Commit commit, SemanticVersion semVer, string tag)
        {
            Tag = tag;
            Commit = commit;
            SemVer = semVer;
        }
    }
}