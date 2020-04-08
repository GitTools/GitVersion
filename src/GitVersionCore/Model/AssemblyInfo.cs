using System.Collections.Generic;

namespace GitVersion
{
    public class AssemblyInfo
    {
        public bool ShouldUpdate;
        public bool EnsureAssemblyInfo;
        public ISet<string> Files = new HashSet<string>();
    }
}
