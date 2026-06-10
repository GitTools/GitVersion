using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

/// <summary>Represents a Git reference name in both its canonical (<c>refs/heads/main</c>) and friendly (<c>main</c>) forms.</summary>
public class ReferenceName : IEquatable<ReferenceName?>, IComparable<ReferenceName>
{
    private static readonly LambdaEqualityHelper<ReferenceName> equalityHelper = new(x => x.Canonical);
    private static readonly LambdaKeyComparer<ReferenceName, string> comparerHelper = new(x => x.Canonical);

    /// <summary>The canonical prefix for local branches.</summary>
    public const string LocalBranchPrefix = "refs/heads/";

    /// <summary>The canonical prefix for remote-tracking branches.</summary>
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

    /// <summary>Initializes a new <see cref="ReferenceName"/> from its canonical form.</summary>
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

    /// <summary>Parses <paramref name="canonicalName"/> as a canonical Git reference name, throwing when the input is not a valid canonical form.</summary>
    public static ReferenceName Parse(string canonicalName)
    {
        if (TryParse(out var value, canonicalName)) return value;
        throw new ArgumentException($"The {nameof(canonicalName)} is not a Canonical name");
    }

    /// <summary>Creates a <see cref="ReferenceName"/> from a branch name, automatically prepending the local-branch prefix when necessary.</summary>
    public static ReferenceName FromBranchName(string branchName)
        => TryParse(out var value, branchName)
            ? value
            : Parse(LocalBranchPrefix + branchName);

    /// <summary>Gets the canonical reference name (e.g. <c>refs/heads/main</c>).</summary>
    public string Canonical { get; }

    /// <summary>Gets the shortened, human-readable reference name (e.g. <c>main</c> or <c>origin/main</c>).</summary>
    public string Friendly { get; }

    /// <summary>Gets the friendly name with the <c>origin/</c> prefix removed for remote-tracking branches.</summary>
    public string WithoutOrigin { get; }

    /// <summary>Gets a value indicating whether this is a local branch reference.</summary>
    public bool IsLocalBranch { get; }

    /// <summary>Gets a value indicating whether this is a remote-tracking branch reference.</summary>
    public bool IsRemoteBranch { get; }

    /// <summary>Gets a value indicating whether this is a tag reference.</summary>
    public bool IsTag { get; }

    /// <summary>Gets a value indicating whether this reference corresponds to a pull request.</summary>
    public bool IsPullRequest { get; }

    /// <summary>Returns <see langword="true"/> when the canonical name of this instance equals that of <paramref name="other"/>.</summary>
    public bool Equals(ReferenceName? other) => equalityHelper.Equals(this, other);

    /// <summary>Compares this instance to <paramref name="other"/> by canonical name.</summary>
    public int CompareTo(ReferenceName? other) => comparerHelper.Compare(this, other);

    /// <summary>Returns <see langword="true"/> when <paramref name="obj"/> is a <see cref="ReferenceName"/> with the same canonical name.</summary>
    public override bool Equals(object? obj) => Equals(obj as ReferenceName);

    /// <summary>Returns a hash code based on the canonical name.</summary>
    public override int GetHashCode() => equalityHelper.GetHashCode(this);

    /// <summary>Returns the friendly (shortened) name of this reference.</summary>
    public override string ToString() => Friendly;

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> and <paramref name="right"/> have equal canonical names.</summary>
    public static bool operator ==(ReferenceName? left, ReferenceName? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="left"/> and <paramref name="right"/> have different canonical names.</summary>
    public static bool operator !=(ReferenceName? left, ReferenceName? right) => !(left == right);

    /// <summary>Returns <see langword="true"/> when <paramref name="name"/> matches any of the canonical, friendly, or origin-stripped forms of this reference.</summary>
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

        return IsTag ? Canonical[TagPrefix.Length..] : Canonical;
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
