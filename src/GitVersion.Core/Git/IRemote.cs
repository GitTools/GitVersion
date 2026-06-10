namespace GitVersion.Git;

/// <summary>Represents a configured Git remote.</summary>
public interface IRemote : IEquatable<IRemote?>, IComparable<IRemote>
{
    /// <summary>Gets the short name of the remote (e.g. <c>origin</c>).</summary>
    string Name { get; }

    /// <summary>Gets the URL of the remote repository.</summary>
    string Url { get; }

    /// <summary>Gets the refspecs used when fetching from this remote.</summary>
    IEnumerable<IRefSpec> FetchRefSpecs { get; }

    /// <summary>Gets the refspecs used when pushing to this remote.</summary>
    IEnumerable<IRefSpec> PushRefSpecs { get; }
}
