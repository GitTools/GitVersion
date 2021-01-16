using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion
{
    internal sealed class Tag : ITag
    {
        private static readonly LambdaEqualityHelper<ITag> equalityHelper = new(x => x.CanonicalName);
        private static readonly LambdaKeyComparer<ITag, string> comparerHelper = new(x => x.CanonicalName);

        private readonly LibGit2Sharp.Tag innerTag;
        internal Tag(LibGit2Sharp.Tag tag) => innerTag = tag;

        public int CompareTo(ITag other) => comparerHelper.Compare(this, other);
        public override bool Equals(object obj) => Equals((obj as ITag)!);
        public bool Equals(ITag other) => equalityHelper.Equals(this, other);
        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public string CanonicalName => innerTag.CanonicalName;
        public string FriendlyName => innerTag.FriendlyName;
        public string TargetSha => innerTag.Target.Sha;

        public ICommit? PeeledTargetCommit()
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
