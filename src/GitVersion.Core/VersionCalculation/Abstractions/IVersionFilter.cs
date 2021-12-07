using System.Diagnostics.CodeAnalysis;

namespace GitVersion.VersionCalculation;

public interface IVersionFilter
{
    bool Exclude(BaseVersion version, [NotNullWhen(true)] out string? reason);
}
