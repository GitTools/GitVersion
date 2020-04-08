using System.Collections.Generic;

namespace GitVersion
{
    public class AssemblyInfo
    {
        public bool UpdateAssemblyInfo;
        public ISet<string> AssemblyInfoFiles = new HashSet<string>();
        public bool EnsureAssemblyInfo;
    }
}
