namespace GitVersion
{
    using LibGit2Sharp;

    public class VersionTaggedCommit
    {
        public Tag Tag;
        public Commit Commit;
        public SemanticVersion SemVer;


        public VersionTaggedCommit(Commit commit, SemanticVersion semVer)
        {
            Commit = commit;
            SemVer = semVer;
        }

        public VersionTaggedCommit(Commit commit, SemanticVersion semVer, Tag tag)
        {
            Tag = tag;
            Commit = commit;
            SemVer = semVer;
        }
    }
}