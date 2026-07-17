using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class ManagedReferenceCollection(ManagedGitRepository repository) : IReferenceCollection
{
    private readonly ManagedGitRepository repository = repository.NotNull();

    public IEnumerator<IReference> GetEnumerator() => this.repository.Session.References.Cast<IReference>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IReference? Head => this["HEAD"];

    public IReference? this[string name]
    {
        get
        {
            name = name.NotNull();
            return this.repository.Session.GetReference(name);
        }
    }

    public IReference? this[ReferenceName referenceName] => this[referenceName.Canonical];

    public IEnumerable<IReference> FromGlob(string prefix)
    {
        prefix = prefix.NotNull();

        // GitVersion only uses trailing-wildcard globs ('*' and 'refs/remotes/<remote>/*'),
        // which map onto a canonical-name prefix enumeration.
        var referencePrefix = prefix.EndsWith('*') ? prefix[..^1] : prefix;

        if (referencePrefix.Length == 0)
        {
            referencePrefix = "refs/";
        }

        return this.repository.Session.EnumerateReferences(referencePrefix);
    }

    public void Add(string name, string canonicalRefNameOrObject, bool allowOverwrite = false)
    {
        name = name.NotNull();
        canonicalRefNameOrObject = canonicalRefNameOrObject.NotNull();

        if (!allowOverwrite && this[name] is not null)
        {
            throw new InvalidOperationException($"A reference with the name '{name}' already exists.");
        }

        var workingDirectory = this.repository.CliWorkingDirectory;

        if (IsObjectId(canonicalRefNameOrObject))
        {
            this.repository.CliMutator.UpdateReference(workingDirectory, name, canonicalRefNameOrObject);
        }
        else
        {
            this.repository.CliMutator.CreateSymbolicReference(workingDirectory, name, canonicalRefNameOrObject);
        }

        this.repository.Invalidate();
    }

    public void UpdateTarget(IReference directRef, IObjectId targetId)
    {
        directRef.NotNull();
        targetId.NotNull();

        this.repository.CliMutator.UpdateReference(this.repository.CliWorkingDirectory, directRef.Name.Canonical, targetId.Sha);
        this.repository.Invalidate();
    }

    private static bool IsObjectId(string value) =>
        value.Length is 40 or 64 && value.All(Uri.IsHexDigit);
}
