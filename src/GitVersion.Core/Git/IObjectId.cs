namespace GitVersion.Git;

/// <summary>Represents the SHA-1 object identifier of a Git object.</summary>
public interface IObjectId : IEquatable<IObjectId?>, IComparable<IObjectId>
{
    /// <summary>Gets the full 40-character hexadecimal SHA-1 string.</summary>
    string Sha { get; }

    /// <summary>Returns a shortened representation of the SHA using the first <paramref name="prefixLength"/> characters.</summary>
    string ToString(int prefixLength);
}
