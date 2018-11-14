namespace GitTools.Git
{
    using LibGit2Sharp;

    public class TaggedCommit
    {
        public TaggedCommit(Commit commit, string tagName)
        {
            Commit = commit;
            TagName = tagName;
        }

        public Commit Commit { get; private set; }
        public string TagName { get; private set; }
    }
}