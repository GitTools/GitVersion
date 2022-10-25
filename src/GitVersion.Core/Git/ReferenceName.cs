using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion;

public class ReferenceName : IEquatable<ReferenceName?>, IComparable<ReferenceName>
{
    private static readonly LambdaEqualityHelper<ReferenceName> equalityHelper = new(x => x.Canonical);
    private static readonly LambdaKeyComparer<ReferenceName, string> comparerHelper = new(x => x.Canonical);

    private const string LocalBranchPrefix = "refs/heads/";
    private const string RemoteTrackingBranchPrefix = "refs/remotes/";
    private const string TagPrefix = "refs/tags/";
    private static readonly string[] PullRequestPrefixes =
    {
        "refs/pull/",
        "refs/pull-requests/",
        "refs/remotes/pull/",
        "refs/remotes/pull-requests/"
    };

    public ReferenceName(string canonical)
    {
        Canonical = canonical.NotNull();

        IsBranch = IsPrefixedBy(Canonical, LocalBranchPrefix);
        IsRemoteBranch = IsPrefixedBy(Canonical, RemoteTrackingBranchPrefix);
        IsTag = IsPrefixedBy(Canonical, TagPrefix);
        IsPullRequest = IsPrefixedBy(Canonical, PullRequestPrefixes);

        Friendly = Shorten();
        WithoutRemote = RemoveRemote();
    }

    public static ReferenceName Parse(string canonicalName)
    {
        if (IsPrefixedBy(canonicalName, LocalBranchPrefix)
            || IsPrefixedBy(canonicalName, RemoteTrackingBranchPrefix)
            || IsPrefixedBy(canonicalName, TagPrefix)
            || IsPrefixedBy(canonicalName, PullRequestPrefixes))
        {
            return new ReferenceName(canonicalName);
        }

        throw new ArgumentException($"The {nameof(canonicalName)} is not a Canonical name");
    }

    public static ReferenceName FromBranchName(string branchName) => Parse(LocalBranchPrefix + branchName);

    public string Canonical { get; }
    public string Friendly { get; }
    public string WithoutRemote { get; }
    public bool IsBranch { get; }
    public bool IsRemoteBranch { get; }
    public bool IsTag { get; }
    public bool IsPullRequest { get; }

    public bool Equals(ReferenceName? other) => equalityHelper.Equals(this, other);
    public int CompareTo(ReferenceName other) => comparerHelper.Compare(this, other);
    public override bool Equals(object obj) => Equals((obj as ReferenceName));
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Friendly;

    public bool EquivalentTo(string? name) =>
        Canonical.Equals(name, StringComparison.OrdinalIgnoreCase)
        || Friendly.Equals(name, StringComparison.OrdinalIgnoreCase)
        || WithoutRemote.Equals(name, StringComparison.OrdinalIgnoreCase);

    private string Shorten()
    {
        if (IsBranch)
            return Canonical.Substring(LocalBranchPrefix.Length);

        if (IsRemoteBranch)
            return Canonical.Substring(RemoteTrackingBranchPrefix.Length);

        if (IsTag)
            return Canonical.Substring(TagPrefix.Length);

        return Canonical;
    }

    private string RemoveRemote()
    {
        if (IsRemoteBranch)
        {
            if (!IsPullRequest)
                return Friendly.Substring(Friendly.IndexOf("/", StringComparison.Ordinal) + 1);
        }

        return Friendly;
    }

    private static bool IsPrefixedBy(string input, string prefix) => input.StartsWith(prefix, StringComparison.Ordinal);

    private static bool IsPrefixedBy(string input, string[] prefixes) => prefixes.Any(prefix => IsPrefixedBy(input, prefix));
}
