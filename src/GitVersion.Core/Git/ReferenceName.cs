using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

public class ReferenceName : IEquatable<ReferenceName?>, IComparable<ReferenceName>
{
    private static readonly LambdaEqualityHelper<ReferenceName> equalityHelper = new(x => x.Canonical);
    private static readonly LambdaKeyComparer<ReferenceName, string> comparerHelper = new(x => x.Canonical);

    public const string LocalBranchPrefix = "refs/heads/";
    public const string RemoteTrackingBranchPrefix = "refs/remotes/";
    private const string TagPrefix = "refs/tags/";
    private const string OriginPrefix = "origin/";

    private static readonly string[] PullRequestPrefixes =
    [
        "refs/pull/",
        "refs/pull-requests/",
        "refs/remotes/pull/",
        "refs/remotes/pull-requests/"
    ];

    public ReferenceName(string canonical)
    {
        Canonical = canonical.NotNull();

        IsLocalBranch = IsPrefixedBy(Canonical, LocalBranchPrefix);
        IsRemoteBranch = IsPrefixedBy(Canonical, RemoteTrackingBranchPrefix);
        IsTag = IsPrefixedBy(Canonical, TagPrefix);
        IsPullRequest = IsPrefixedBy(Canonical, PullRequestPrefixes);

        Friendly = Shorten();
        WithoutOrigin = RemoveOrigin();
    }

    public static ReferenceName Parse(string canonicalName)
    {
        if (TryParse(out var value, canonicalName)) return value;
        throw new ArgumentException($"The {nameof(canonicalName)} is not a Canonical name");
    }

    public static ReferenceName FromBranchName(string branchName)
        => TryParse(out var value, branchName)
            ? value
            : Parse(LocalBranchPrefix + branchName);

    public string Canonical { get; }

    public string Friendly { get; }

    public string WithoutOrigin { get; }

    public bool IsLocalBranch { get; }

    public bool IsRemoteBranch { get; }

    public bool IsTag { get; }

    public bool IsPullRequest { get; }

    public bool Equals(ReferenceName? other) => equalityHelper.Equals(this, other);

    public int CompareTo(ReferenceName? other) => comparerHelper.Compare(this, other);

    public override bool Equals(object? obj) => Equals(obj as ReferenceName);

    public override int GetHashCode() => equalityHelper.GetHashCode(this);

    public override string ToString() => Friendly;

    public static bool operator ==(ReferenceName? left, ReferenceName? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ReferenceName? left, ReferenceName? right) => !(left == right);

    public bool EquivalentTo(string? name) =>
        Canonical.Equals(name, StringComparison.OrdinalIgnoreCase)
        || Friendly.Equals(name, StringComparison.OrdinalIgnoreCase)
        || WithoutOrigin.Equals(name, StringComparison.OrdinalIgnoreCase);

    private string Shorten()
    {
        if (IsLocalBranch)
            return Canonical[LocalBranchPrefix.Length..];

        if (IsRemoteBranch)
            return Canonical[RemoteTrackingBranchPrefix.Length..];

        if (IsTag)
            return Canonical[TagPrefix.Length..];

        return Canonical;
    }

    private string RemoveOrigin()
    {
        if (IsRemoteBranch && !IsPullRequest && Friendly.StartsWith(OriginPrefix, StringComparison.Ordinal))
        {
            return Friendly[OriginPrefix.Length..];
        }

        return Friendly;
    }

    private static bool TryParse([NotNullWhen(true)] out ReferenceName? value, string canonicalName)
    {
        value = null;

        if (IsPrefixedBy(canonicalName, LocalBranchPrefix)
            || IsPrefixedBy(canonicalName, RemoteTrackingBranchPrefix)
            || IsPrefixedBy(canonicalName, TagPrefix)
            || IsPrefixedBy(canonicalName, PullRequestPrefixes))
        {
            value = new(canonicalName);
        }

        return value is not null;
    }

    private static bool IsPrefixedBy(string input, string prefix) => input.StartsWith(prefix, StringComparison.Ordinal);

    private static bool IsPrefixedBy(string input, string[] prefixes) => prefixes.Any(prefix => IsPrefixedBy(input, prefix));
}
