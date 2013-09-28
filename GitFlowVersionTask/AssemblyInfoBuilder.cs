namespace GitFlowVersionTask
{
    using GitFlowVersion;

    public  class AssemblyInfoBuilder
    {

        public VersionAndBranch VersionAndBranch;
        public bool SignAssembly;

        public string GetAssemblyInfoText()
        {
            var assemblyVersion = GetAssemblyVersion();
            var assemblyFileVersion = GetAssemblyFileVersion();
            var assemblyInfo = string.Format(@"
using System.Reflection;
[assembly: AssemblyVersion(""{0}"")]
[assembly: AssemblyFileVersion(""{1}"")]
[assembly: AssemblyInformationalVersion(""{2}"")]
", assemblyVersion, assemblyFileVersion, VersionAndBranch.ToLongString());
            return assemblyInfo;
        }

        string GetAssemblyVersion()
        {
            var semanticVersion = VersionAndBranch.Version;
            if (SignAssembly)
            {
                // for strong named we don't want to include the patch to avoid binding redirect issues
                return string.Format("{0}.{1}.0", semanticVersion.Major, semanticVersion.Minor);
            }
            // for non strong named we want to include the patch
            return string.Format("{0}.{1}.{2}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch);
        }

        string GetAssemblyFileVersion()
        {
            var semanticVersion = VersionAndBranch.Version;

            return string.Format("{0}.{1}.{2}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch);
        }
    }
}