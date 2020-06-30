using System.Collections.Generic;

namespace GitVersion
{
    public class AssemblyInfoData
    {
        public bool UpdateAssemblyInfo;
        public bool UpdateProjectFiles;
        public bool EnsureAssemblyInfo;
        public ISet<string> Files = new HashSet<string>();
    }
}
