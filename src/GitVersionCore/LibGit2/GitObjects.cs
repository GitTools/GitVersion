using System;
using System.Collections;
using System.Collections.Generic;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion
{
    public class Commit : ICommit
    {
        private static readonly LambdaEqualityHelper<ICommit> equalityHelper =
            new LambdaEqualityHelper<ICommit>(x => x.Id);

        private readonly LibGit2Sharp.Commit innerObjectId;

        private Commit(LibGit2Sharp.Commit objectId)
        {
            innerObjectId = objectId;
        }

        protected Commit()
        {
        }

        public override bool Equals(object obj) => Equals(obj as ICommit);
        private bool Equals(ICommit other) => equalityHelper.Equals(this, other);

        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public static bool operator !=(Commit left, Commit right) => !Equals(left, right);
        public static bool operator ==(Commit left, Commit right) => Equals(left, right);

        public static implicit operator LibGit2Sharp.Commit(Commit d) => d?.innerObjectId;
        public static explicit operator Commit(LibGit2Sharp.Commit b) => b is null ? null : new Commit(b);

        public virtual IEnumerable<ICommit> Parents
        {
            get
            {
                if (innerObjectId == null) yield return null;
                else
                    foreach (var parent in innerObjectId.Parents)
                        yield return (Commit)parent;
            }
        }

        public virtual string Sha => innerObjectId?.Sha;

        public virtual IObjectId Id
        {
            get
            {
                var objectId = innerObjectId?.Id;
                return objectId is null ? null : new ObjectId(objectId);
            }
        }

        public virtual DateTimeOffset? CommitterWhen => innerObjectId?.Committer.When;
        public virtual string Message => innerObjectId?.Message;
    }

    public class CommitCollection : IEnumerable<ICommit>
    {
        private readonly ICommitLog innerCollection;
        private CommitCollection(ICommitLog collection) => innerCollection = collection;

        protected CommitCollection()
        {
        }

        public virtual IEnumerator<ICommit> GetEnumerator()
        {
            foreach (var commit in innerCollection)
                yield return (Commit)commit;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal static CommitCollection FromCommitLog(ICommitLog b) => b is null ? null : new CommitCollection(b);
        public virtual CommitCollection QueryBy(CommitFilter commitFilter)
        {
            static object GetReacheableFrom(object item)
            {
                return item switch
                {
                    Commit c => (LibGit2Sharp.Commit)c,
                    Branch b => (LibGit2Sharp.Branch)b,
                    _ => null
                };
            }

            var includeReachableFrom = GetReacheableFrom(commitFilter.IncludeReachableFrom);
            var excludeReachableFrom = GetReacheableFrom(commitFilter.ExcludeReachableFrom);
            var filter = new LibGit2Sharp.CommitFilter
            {
                IncludeReachableFrom = includeReachableFrom,
                ExcludeReachableFrom = excludeReachableFrom,
                FirstParentOnly = commitFilter.FirstParentOnly,
                SortBy = (LibGit2Sharp.CommitSortStrategies)commitFilter.SortBy,
            };
            var commitLog = ((IQueryableCommitLog)innerCollection).QueryBy(filter);
            return FromCommitLog(commitLog);
        }
    }
}
