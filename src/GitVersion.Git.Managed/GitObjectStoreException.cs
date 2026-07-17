using System.Diagnostics.CodeAnalysis;

namespace GitVersion.Git;

/// <summary>
/// The exception thrown by the managed Git object store when the object database
/// is malformed or a requested object cannot be found.
/// </summary>
[SuppressMessage("Critical Code Smell", "S3871:Exception types should be \"public\"", Justification = "Every type in this vendored library is internal by design (Phase B.1); the exception never crosses the assembly boundary.")]
internal sealed class GitObjectStoreException : Exception
{
    public GitObjectStoreException()
    {
    }

    public GitObjectStoreException(string message) : base(message)
    {
    }

    public GitObjectStoreException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets a value indicating whether this exception represents a missing object
    /// (as opposed to a malformed object database).
    /// </summary>
    public bool ObjectNotFound { get; init; }
}
