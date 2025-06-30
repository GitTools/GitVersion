namespace GitVersion.Testing.Helpers;

/// <summary>
/// Generates unique uppercase letter labels (A, B, ..., Z, AA, AB, etc.) for unique keys.
/// </summary>
public class CommitLabelGenerator
{
    private readonly Dictionary<string, string> lookup = new();

    /// <summary>
    /// Gets the label assigned to the specified key, creating a new one if not already assigned.
    /// </summary>
    public string GetOrAdd(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("SHA cannot be empty or whitespace.", nameof(key));
        }

        if (key == "N/A")
        {
            return "N/A";
        }

        if (lookup.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var label = GetNextLabel();
        lookup[key] = label;
        return label;
    }

    public string GetOrAddRoot(string? versionSourceSha)
    {
        if (string.IsNullOrWhiteSpace(versionSourceSha))
        {
            throw new ArgumentException("Version source SHA cannot be empty or whitespace.", nameof(versionSourceSha));
        }

        if (versionSourceSha == "N/A")
        {
            return "N/A";
        }

        return GetOrAddRootPrivate(versionSourceSha);
    }

    private string GetOrAddRootPrivate(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("SHA cannot be empty or whitespace.", nameof(key));
        }

        if (key == "N/A")
        {
            return "N/A";
        }

        if (lookup.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var label = GetNextRootLabel();
        lookup[key] = label;
        return label;
    }

    private string GetNextRootLabel() => "Root" + GetNextLabel();

    private string GetNextLabel()
    {
        var index = lookup.Count;
        var label = string.Empty;

        do
        {
            label = (char)('A' + (index % 26)) + label;
            index = (index / 26) - 1;
        } while (index >= 0);

        return label;
    }
}
