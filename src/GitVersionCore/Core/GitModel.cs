using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public virtual string TargetSha => innerTag?.Target.Sha;
        public virtual string FriendlyName => innerTag?.FriendlyName;

        public Commit PeeledTargetCommit()
        {
            var target = innerTag.Target;

            while (target is TagAnnotation annotation)
            {
                target = annotation.Target;
            }

            return target is LibGit2Sharp.Commit commit ? (Commit)commit : null;
        }
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
        public virtual DateTimeOffset? CommitterWhen => innerCommit?.Committer.When;
        public virtual string Message => innerCommit?.Message;
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

    public class Reference
    {
        private readonly LibGit2Sharp.Reference innerReference;
        private DirectReference directReference => innerReference.ResolveToDirectReference();

        private Reference(LibGit2Sharp.Reference reference)
        {
            innerReference = reference;
        }

        protected Reference()
        {
        }
        public virtual string CanonicalName => innerReference.CanonicalName;
        public virtual string TargetIdentifier => innerReference.TargetIdentifier;
        public virtual string DirectReferenceTargetIdentifier => directReference.TargetIdentifier;
        public virtual ObjectId DirectReferenceTargetId => directReference.Target.Id;

        public virtual Reference ResolveToDirectReference() => (Reference)directReference;
        public static implicit operator LibGit2Sharp.Reference(Reference d) => d?.innerReference;
        public static explicit operator Reference(LibGit2Sharp.Reference b) => b is null ? null : new Reference(b);
    }

    public class Remote
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
                yield return (Reference)reference;
        }

        public virtual Reference Add(string name, string canonicalRefNameOrObjectish)
        {
            return (Reference)innerCollection.Add(name, canonicalRefNameOrObjectish);
        }

        public virtual void Add(string name, ObjectId targetId)
        {
            innerCollection.Add(name, targetId);
        }

        public virtual void Add(string name, ObjectId targetId, bool allowOverwrite)
        {
            innerCollection.Add(name, targetId, allowOverwrite);
        }

        public virtual Reference UpdateTarget(Reference directRef, ObjectId targetId)
        {
            return (Reference)innerCollection.UpdateTarget(directRef, targetId);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public virtual Reference this[string name] => (Reference)innerCollection[name];
        public virtual Reference Head => this["HEAD"];

        public virtual IEnumerable<Reference> FromGlob(string pattern)
        {
            foreach (var reference in innerCollection.FromGlob(pattern))
                yield return (Reference)reference;
        }
    }

    public class CommitCollection : IEnumerable<Commit>
    {
        private readonly ICommitLog innerCollection;
        private CommitCollection(ICommitLog collection) => innerCollection = collection;

        protected CommitCollection()
        {
        }

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
