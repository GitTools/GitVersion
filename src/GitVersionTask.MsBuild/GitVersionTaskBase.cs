using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace GitVersionTask.MsBuild
{
    public abstract class GitVersionTaskBase : Task
    {
        [Required]
        public string SolutionDirectory { get; set; }

        public string ConfigFilePath { get; set; }

        public bool NoFetch { get; set; }
    }
}
