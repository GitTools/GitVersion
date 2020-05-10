using System.Collections.Generic;

namespace GitVersion
{
    public class AssemblyInfoData
    {
        public bool IsTargetingProjectFiles;
        public bool ShouldUpdate;
        public bool EnsureAssemblyInfo;
        public ISet<string> Files = new HashSet<string>();
    }
}
