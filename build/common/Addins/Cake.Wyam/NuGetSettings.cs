#nullable disable
namespace Common.Addins.Cake.Wyam;

/// <summary>
/// Settings for specifying NuGet packages.
/// </summary>
public class NuGetSettings
{
    /// <summary>
    /// Specifies that prerelease packages are allowed.
    /// </summary>
    public bool Prerelease { get; set; }

    /// <summary>
    /// Specifies that unlisted packages are allowed.
    /// </summary>
    public bool Unlisted { get; set; }

    /// <summary>
    /// Indicates that only the specified package source(s) should be used to find the package.
    /// </summary>
    public bool Exclusive { get; set; }

    /// <summary>
    /// Specifies the version of the package to use.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Specifies the package source(s) to get the package from.
    /// </summary>
    public IEnumerable<string> Source { get; set; }

    /// <summary>
    /// The package to install.
    /// </summary>
    public string Package { get; set; }
}
