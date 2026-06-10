namespace GitVersion.Git;

/// <summary>Represents the set of all references in a Git repository.</summary>
public interface IReferenceCollection : IEnumerable<IReference>
{
    /// <summary>Gets the current HEAD reference, or <see langword="null"/> when HEAD is unborn.</summary>
    IReference? Head { get; }

    /// <summary>Returns the reference with the given canonical <paramref name="name"/>, or <see langword="null"/> if it does not exist.</summary>
    IReference? this[string name] { get; }

    /// <summary>Returns the reference with the given <paramref name="referenceName"/>, or <see langword="null"/> if it does not exist.</summary>
    IReference? this[ReferenceName referenceName] { get; }

    /// <summary>Creates a new reference named <paramref name="name"/> pointing to <paramref name="canonicalRefNameOrObject"/>.</summary>
    void Add(string name, string canonicalRefNameOrObject, bool allowOverwrite = false);

    /// <summary>Updates the target of the direct reference <paramref name="directRef"/> to point to <paramref name="targetId"/>.</summary>
    void UpdateTarget(IReference directRef, IObjectId targetId);

    /// <summary>Returns all references whose canonical names start with <paramref name="prefix"/>.</summary>
    IEnumerable<IReference> FromGlob(string prefix);
}
