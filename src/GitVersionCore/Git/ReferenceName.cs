using System;
using GitVersion.Helpers;

namespace GitVersion
{
    public class ReferenceName : IEquatable<ReferenceName>, IComparable<ReferenceName>
    {
        private static readonly LambdaEqualityHelper<ReferenceName> equalityHelper = new(x => x.Canonical);
        private static readonly LambdaKeyComparer<ReferenceName, string> comparerHelper = new(x => x.Canonical);

        private const string LocalBranchPrefix = "refs/heads/";
        private const string RemoteTrackingBranchPrefix = "refs/remotes/";
        private const string TagPrefix = "refs/tags/";

        public ReferenceName(string canonical)
        {
            Canonical = canonical ?? throw new ArgumentNullException(nameof(canonical));
            Friendly = Shorten();
            WithoutRemote = RemoveRemote();
        }

        public static ReferenceName Parse(string canonicalName)
        {
            if (IsPrefixedBy(canonicalName, LocalBranchPrefix)
                || IsPrefixedBy(canonicalName, RemoteTrackingBranchPrefix)
                || IsPrefixedBy(canonicalName, TagPrefix))
            {
                return new ReferenceName(canonicalName);
            }
            throw new ArgumentException($"The {nameof(canonicalName)} is not a Canonical name");
        }
        public string Canonical { get; }
        public string Friendly { get; }
        public string WithoutRemote { get; }

        public bool Equals(ReferenceName other) => equalityHelper.Equals(this, other);
        public int CompareTo(ReferenceName other) => comparerHelper.Compare(this, other);
        public override bool Equals(object obj) => Equals((obj as ReferenceName)!);
        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public override string ToString() => Friendly;

        public bool EquivalentTo(string name)
        {
            return Canonical.Equals(name, StringComparison.OrdinalIgnoreCase)
                   || Friendly.Equals(name, StringComparison.OrdinalIgnoreCase)
                   || WithoutRemote.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        private string Shorten()
        {
            if (IsPrefixedBy(Canonical, LocalBranchPrefix))
                return Canonical.Substring(LocalBranchPrefix.Length);
            if (IsPrefixedBy(Canonical, RemoteTrackingBranchPrefix))
                return Canonical.Substring(RemoteTrackingBranchPrefix.Length);
            if (IsPrefixedBy(Canonical, TagPrefix))
                return Canonical.Substring(TagPrefix.Length);
            return Canonical;
        }

        private string RemoveRemote()
        {
            var isRemote = IsPrefixedBy(Canonical, RemoteTrackingBranchPrefix);

            return isRemote
                ? Friendly.Substring(Friendly.IndexOf("/", StringComparison.Ordinal) + 1)
                : Friendly;
        }
        private static bool IsPrefixedBy(string input, string prefix) => input.StartsWith(prefix, StringComparison.Ordinal);
    }
}
