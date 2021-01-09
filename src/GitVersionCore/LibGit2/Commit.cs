using System;
using System.Collections.Generic;
using GitVersion.Helpers;

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

}
