using Microsoft.Build.Framework;

namespace GitVersion.MSBuildTask
{
    public abstract class GitVersionTaskBase : ITask
    {
        protected GitVersionTaskBase()
        {
            this.Log = new TaskLoggingHelper(this);
        }

        [Required]
        public string SolutionDirectory { get; set; }

        public string ConfigFilePath { get; set; }

        public bool NoFetch { get; set; }

        public bool NoNormalize { get; set; }
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }
        public TaskLoggingHelper Log { get; }

        public abstract bool Execute();       

    }
}
