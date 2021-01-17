using System;

namespace GitVersion
{
    public class ReferenceName
    {
        private const string LocalBranchPrefix = "refs/heads/";
        private const string RemoteTrackingBranchPrefix = "refs/remotes/";
        private const string TagPrefix = "refs/tags/";

        public ReferenceName(string canonicalName)
        {
            CanonicalName = canonicalName;
            FriendlyName = Shorten();
            NameWithoutRemote = RemoveRemote();
        }
        public string CanonicalName { get; }
        public string FriendlyName { get; }
        public string NameWithoutRemote { get; }

        private string Shorten()
        {
            if (IsPrefixedBy(CanonicalName, LocalBranchPrefix))
                return CanonicalName.Substring(LocalBranchPrefix.Length);
            if (IsPrefixedBy(CanonicalName, RemoteTrackingBranchPrefix))
                return CanonicalName.Substring(RemoteTrackingBranchPrefix.Length);
            if (IsPrefixedBy(CanonicalName, TagPrefix))
                return CanonicalName.Substring(TagPrefix.Length);
            return CanonicalName;
        }

        private string RemoveRemote()
        {
            var isRemote = IsPrefixedBy(CanonicalName, RemoteTrackingBranchPrefix);

            return isRemote
                ? FriendlyName.Substring(FriendlyName.IndexOf("/", StringComparison.Ordinal) + 1)
                : FriendlyName;
        }
        private static bool IsPrefixedBy(string input, string prefix) => input.StartsWith(prefix, StringComparison.Ordinal);
    }
}
