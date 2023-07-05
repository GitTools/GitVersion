namespace GitVersion;

public class AssemblySettingsInfo
{
    public bool UpdateAssemblyInfo;
    public bool UpdateProjectFiles;
    public bool EnsureAssemblyInfo;
    public ISet<string> Files = new HashSet<string>();
}
