using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion
{
    internal sealed class Tag : ITag
    {
        private static readonly LambdaEqualityHelper<ITag> equalityHelper = new(x => x.Name.CanonicalName);
        private static readonly LambdaKeyComparer<ITag, string> comparerHelper = new(x => x.Name.CanonicalName);

        private readonly LibGit2Sharp.Tag innerTag;
        internal Tag(LibGit2Sharp.Tag tag)
        {
            innerTag = tag;
            Name = new ReferenceName(innerTag.CanonicalName);
        }
        public ReferenceName Name { get; }

        public int CompareTo(ITag other) => comparerHelper.Compare(this, other);
        public override bool Equals(object obj) => Equals((obj as ITag)!);
        public bool Equals(ITag other) => equalityHelper.Equals(this, other);
        public override int GetHashCode() => equalityHelper.GetHashCode(this);
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
