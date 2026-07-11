// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// Enumerates the object types which can be stored in a Git pack file.
/// </summary>
/// <seealso href="https://git-scm.com/docs/pack-format"/>
internal enum GitPackObjectType
{
    /// <summary>Invalid.</summary>
    Invalid = 0,

    /// <summary>A commit.</summary>
    Commit = 1,

    /// <summary>A tree.</summary>
    Tree = 2,

    /// <summary>A blob.</summary>
    Blob = 3,

    /// <summary>A tag.</summary>
    Tag = 4,

    /// <summary>A deltified object whose base object is referenced by a relative offset in the same pack.</summary>
    OfsDelta = 6,

    /// <summary>A deltified object whose base object is referenced by object id.</summary>
    RefDelta = 7
}
