namespace GitVersion;

/// <summary>Contains options that control how GitVersion updates assembly version attributes and project files.</summary>
public class AssemblySettingsInfo
{
    /// <summary>Gets or sets a value indicating whether <c>AssemblyInfo.cs</c> files should be updated with the calculated version.</summary>
    public bool UpdateAssemblyInfo;

    /// <summary>Gets or sets a value indicating whether SDK-style project files (<c>.csproj</c>) should be updated with the calculated version.</summary>
    public bool UpdateProjectFiles;

    /// <summary>Gets or sets a value indicating whether missing <c>AssemblyInfo.cs</c> files should be created automatically.</summary>
    public bool EnsureAssemblyInfo;

    /// <summary>Gets or sets the set of specific assembly-info or project file paths to update.</summary>
    public ISet<string> Files = new HashSet<string>();
}
