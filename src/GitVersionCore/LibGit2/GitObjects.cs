using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion
{
    public class ObjectId : IObjectId
    {
        private static readonly LambdaEqualityHelper<IObjectId> equalityHelper =
            new LambdaEqualityHelper<IObjectId>(x => x.Sha);

        private readonly LibGit2Sharp.ObjectId innerObjectId;
        private ObjectId(LibGit2Sharp.ObjectId objectId)
        {
            innerObjectId = objectId;
        }

        public ObjectId(string sha)
        {
            innerObjectId = new LibGit2Sharp.ObjectId(sha);
        }

        public override bool Equals(object obj) => Equals(obj as IObjectId);
        private bool Equals(IObjectId other) => equalityHelper.Equals(this, other);

        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public static bool operator !=(ObjectId left, ObjectId right) => !Equals(left, right);
        public static bool operator ==(ObjectId left, ObjectId right) => Equals(left, right);
        public static implicit operator LibGit2Sharp.ObjectId(ObjectId d) => d?.innerObjectId;
        public static explicit operator ObjectId(LibGit2Sharp.ObjectId b) => b is null ? null : new ObjectId(b);
        public string Sha => innerObjectId?.Sha;

        public string ToString(int prefixLength) => innerObjectId.ToString(prefixLength);
    }

    public class Commit : ICommit
    {
        private static readonly LambdaEqualityHelper<ICommit> equalityHelper =
            new LambdaEqualityHelper<ICommit>(x => x.Id);

        private readonly LibGit2Sharp.Commit innerCommit;

        private Commit(LibGit2Sharp.Commit commit)
        {
            innerCommit = commit;
        }

        protected Commit()
        {
        }

        public override bool Equals(object obj) => Equals(obj as ICommit);
        private bool Equals(ICommit other) => equalityHelper.Equals(this, other);

        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public static bool operator !=(Commit left, Commit right) => !Equals(left, right);
        public static bool operator ==(Commit left, Commit right) => Equals(left, right);

        public static implicit operator LibGit2Sharp.Commit(Commit d) => d?.innerCommit;
        public static explicit operator Commit(LibGit2Sharp.Commit b) => b is null ? null : new Commit(b);

        public virtual IEnumerable<ICommit> Parents
        {
            get
            {
                if (innerCommit == null) yield return null;
                else
                    foreach (var parent in innerCommit.Parents)
                        yield return (Commit)parent;
            }
        }

        public virtual string Sha => innerCommit?.Sha;
        public virtual IObjectId Id => (ObjectId)innerCommit?.Id;
        public virtual DateTimeOffset? CommitterWhen => innerCommit?.Committer.When;
        public virtual string Message => innerCommit?.Message;
    }

    public class Branch : IBranch
    {
        private readonly LibGit2Sharp.Branch innerBranch;

        private Branch(LibGit2Sharp.Branch branch)
        {
            innerBranch = branch;
        }

        protected Branch()
        {
        }
        public static implicit operator LibGit2Sharp.Branch(Branch d) => d?.innerBranch;
        public static explicit operator Branch(LibGit2Sharp.Branch b) => b is null ? null : new Branch(b);

        public virtual string CanonicalName => innerBranch?.CanonicalName;
        public virtual string FriendlyName => innerBranch?.FriendlyName;
        public virtual ICommit Tip => (Commit)innerBranch?.Tip;
        public virtual CommitCollection Commits => CommitCollection.FromCommitLog(innerBranch?.Commits);
        public virtual bool IsRemote => innerBranch != null && innerBranch.IsRemote;
        public virtual bool IsTracking => innerBranch != null && innerBranch.IsTracking;
    }

    public class Remote : IRemote
    {
        private readonly LibGit2Sharp.Remote innerRemote;

        private Remote(LibGit2Sharp.Remote remote)
        {
            innerRemote = remote;
        }

        protected Remote()
        {
        }
        public static implicit operator LibGit2Sharp.Remote(Remote d) => d?.innerRemote;
        public static explicit operator Remote(LibGit2Sharp.Remote b) => b is null ? null : new Remote(b);
        public virtual string Name => innerRemote.Name;
        public virtual string RefSpecs => string.Join(", ", innerRemote.FetchRefSpecs.Select(r => r.Specification));
    }

    public class BranchCollection : IEnumerable<IBranch>
    {
        private readonly LibGit2Sharp.BranchCollection innerCollection;
        private BranchCollection(LibGit2Sharp.BranchCollection collection) => innerCollection = collection;

        protected BranchCollection()
        {
        }

        public static implicit operator LibGit2Sharp.BranchCollection(BranchCollection d) => d.innerCollection;
        public static explicit operator BranchCollection(LibGit2Sharp.BranchCollection b) => b is null ? null : new BranchCollection(b);

        public virtual IEnumerator<IBranch> GetEnumerator()
        {
            foreach (var branch in innerCollection)
                yield return (Branch)branch;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public virtual IBranch this[string friendlyName] => (Branch)innerCollection[friendlyName];

        public virtual IBranch Add(string name, ICommit commit)
        {
            return (Branch)innerCollection.Add(name, (Commit)commit);
        }
        public void Update(IBranch branch, params Action<BranchUpdater>[] actions)
        {
            innerCollection.Update((Branch)branch, actions);
        }
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
