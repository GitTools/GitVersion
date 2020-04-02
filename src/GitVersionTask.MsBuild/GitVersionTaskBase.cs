using GitVersionTask.MsBuild;
using Microsoft.Build.Framework;

namespace GitVersion.MSBuildTask
{
    public abstract class GitVersionTaskBase : ITask
    {
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        protected GitVersionTaskBase()
        {
            Log = new TaskLoggingHelper(this);
        }

        [Required]
        public string SolutionDirectory { get; set; }

        public string ConfigFilePath { get; set; }

        public bool NoFetch { get; set; }

        public bool NoNormalize { get; set; }

        public TaskLoggingHelper Log { get; }

        public bool Execute()
        {
            if (TaskProxy.InitialiseException != null)
            {
                throw TaskProxy.InitialiseException;
            }

            return OnExecute();
        }

        protected abstract bool OnExecute();
    }
}
