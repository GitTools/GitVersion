using LibGit2Sharp;

namespace GitVersion
{
    public class Tag : ITag
    {
        private readonly LibGit2Sharp.Tag innerTag;
        internal Tag(LibGit2Sharp.Tag tag)
        {
            innerTag = tag;
        }

        protected Tag()
        {
        }
        public virtual string TargetSha => innerTag?.Target.Sha;
        public virtual string FriendlyName => innerTag?.FriendlyName;

        public ICommit PeeledTargetCommit()
        {
            var target = innerTag.Target;

            while (target is TagAnnotation annotation)
            {
                target = annotation.Target;
            }

            return target is LibGit2Sharp.Commit commit ? new Commit(commit) : null;
        }
    }
}
