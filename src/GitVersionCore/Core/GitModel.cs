using System;
using System.Collections;
using System.Collections.Generic;
using GitVersion.Helpers;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace GitVersion
{
    public class AuthenticationInfo
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public CredentialsHandler CredentialsProvider()
        {
            if (!string.IsNullOrWhiteSpace(Username))
            {
                return (url, user, types) => new UsernamePasswordCredentials
                {
                    Username = Username,
                    Password = Password ?? string.Empty
                };
            }
            return null;
        }
        public FetchOptions ToFetchOptions()
        {
            var fetchOptions = new FetchOptions
            {
                CredentialsProvider = CredentialsProvider()
            };

            return fetchOptions;
        }
        public CloneOptions ToCloneOptions()
        {
            var cloneOptions = new CloneOptions
            {
                Checkout = false,
                CredentialsProvider = CredentialsProvider()
            };

            return cloneOptions;
        }
    }

    public class Tag
    {
        private readonly LibGit2Sharp.Tag innerTag;
        private Tag(LibGit2Sharp.Tag tag)
        {
            innerTag = tag;
        }

        protected Tag()
        {
        }
        public static implicit operator LibGit2Sharp.Tag(Tag d) => d?.innerTag;
        public static explicit operator Tag(LibGit2Sharp.Tag b) => b is null ? null : new Tag(b);

        public virtual GitObject Target => innerTag?.Target;
        public virtual string FriendlyName => innerTag?.FriendlyName;
        public virtual TagAnnotation Annotation => innerTag?.Annotation;
    }

    public class Commit
    {
        private static readonly LambdaEqualityHelper<Commit> equalityHelper =
            new LambdaEqualityHelper<Commit>(x => x.Id);

        private readonly LibGit2Sharp.Commit innerCommit;

        private Commit(LibGit2Sharp.Commit commit)
        {
            innerCommit = commit;
        }

        protected Commit()
        {
        }

        public override bool Equals(object obj) => Equals(obj as Commit);
        private bool Equals(Commit other) => equalityHelper.Equals(this, other);

        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public static bool operator !=(Commit left, Commit right) => !Equals(left, right);
        public static bool operator ==(Commit left, Commit right) => Equals(left, right);

        public static implicit operator LibGit2Sharp.Commit(Commit d) => d?.innerCommit;
        public static explicit operator Commit(LibGit2Sharp.Commit b) => b is null ? null : new Commit(b);

        public virtual IEnumerable<Commit> Parents
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
        public virtual ObjectId Id => innerCommit?.Id;
        public virtual Signature Committer => innerCommit?.Committer;
        public virtual string Message => innerCommit?.Message;
        public virtual Tree Tree => innerCommit?.Tree;
    }

    public class Branch
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
        public virtual Commit Tip => (Commit)innerBranch?.Tip;
        public virtual CommitCollection Commits => CommitCollection.FromCommitLog(innerBranch?.Commits);
        public virtual bool IsRemote => innerBranch != null && innerBranch.IsRemote;
        public virtual bool IsTracking => innerBranch != null && innerBranch.IsTracking;
    }

    public class BranchCollection : IEnumerable<Branch>
    {
        private readonly LibGit2Sharp.BranchCollection innerCollection;
        private BranchCollection(LibGit2Sharp.BranchCollection collection) => innerCollection = collection;

        protected BranchCollection()
        {
        }

        public static implicit operator LibGit2Sharp.BranchCollection(BranchCollection d) => d.innerCollection;
        public static explicit operator BranchCollection(LibGit2Sharp.BranchCollection b) => b is null ? null : new BranchCollection(b);

        public virtual IEnumerator<Branch> GetEnumerator()
        {
            foreach (var branch in innerCollection)
                yield return (Branch)branch;
        }

        public virtual Branch Add(string name, Commit commit)
        {
            return (Branch)innerCollection.Add(name, commit);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public virtual Branch this[string friendlyName] => (Branch)innerCollection[friendlyName];

        public void Update(Branch branch, params Action<BranchUpdater>[] actions)
        {
            innerCollection.Update(branch, actions);
        }
    }

    public class TagCollection : IEnumerable<Tag>
    {
        private readonly LibGit2Sharp.TagCollection innerCollection;
        private TagCollection(LibGit2Sharp.TagCollection collection) => innerCollection = collection;

        protected TagCollection()
        {
        }

        public static implicit operator LibGit2Sharp.TagCollection(TagCollection d) => d.innerCollection;
        public static explicit operator TagCollection(LibGit2Sharp.TagCollection b) => b is null ? null : new TagCollection(b);

        public virtual IEnumerator<Tag> GetEnumerator()
        {
            foreach (var tag in innerCollection)
                yield return (Tag)tag;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public virtual Tag this[string name] => (Tag)innerCollection[name];
    }

    public class ReferenceCollection : IEnumerable<Reference>
    {
        private readonly LibGit2Sharp.ReferenceCollection innerCollection;
        private ReferenceCollection(LibGit2Sharp.ReferenceCollection collection) => innerCollection = collection;

        protected ReferenceCollection()
        {
        }

        public static implicit operator LibGit2Sharp.ReferenceCollection(ReferenceCollection d) => d.innerCollection;
        public static explicit operator ReferenceCollection(LibGit2Sharp.ReferenceCollection b) => b is null ? null : new ReferenceCollection(b);

        public IEnumerator<Reference> GetEnumerator()
        {
            foreach (var reference in innerCollection)
                yield return reference;
        }

        public virtual Reference Add(string name, string canonicalRefNameOrObjectish)
        {
            return innerCollection.Add(name, canonicalRefNameOrObjectish);
        }

        public virtual DirectReference Add(string name, ObjectId targetId)
        {
            return innerCollection.Add(name, targetId);
        }

        public virtual DirectReference Add(string name, ObjectId targetId, bool allowOverwrite)
        {
            return innerCollection.Add(name, targetId, allowOverwrite);
        }

        public virtual Reference UpdateTarget(Reference directRef, ObjectId targetId)
        {
            return innerCollection.UpdateTarget(directRef, targetId);
        }

        public virtual ReflogCollection Log(string canonicalName)
        {
            return innerCollection.Log(canonicalName);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public virtual Reference this[string name] => innerCollection[name];
        public virtual Reference Head => this["HEAD"];

        public virtual IEnumerable<Reference> FromGlob(string pattern)
        {
            return innerCollection.FromGlob(pattern);
        }
    }

    public class CommitCollection : IEnumerable<Commit>
    {
        private readonly ICommitLog innerCollection;
        private CommitCollection(ICommitLog collection) => innerCollection = collection;

        protected CommitCollection()
        {
        }

        public static ICommitLog ToCommitLog(CommitCollection d) => d.innerCollection;

        public static CommitCollection FromCommitLog(ICommitLog b) => b is null ? null : new CommitCollection(b);

        public virtual IEnumerator<Commit> GetEnumerator()
        {
            foreach (var commit in innerCollection)
                yield return (Commit)commit;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public virtual CommitCollection QueryBy(CommitFilter commitFilter)
        {
            var commitLog = ((IQueryableCommitLog)innerCollection).QueryBy(commitFilter);
            return FromCommitLog(commitLog);
        }
    }
}
