namespace GitVersion.Git;

/// <summary>Represents any Git object (branch, tag, or ref) that has a canonical reference name.</summary>
public interface INamedReference
{
    /// <summary>Gets the canonical name of this reference.</summary>
    ReferenceName Name { get; }
}
