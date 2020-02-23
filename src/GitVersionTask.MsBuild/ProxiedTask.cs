using Microsoft.Build.Framework;

namespace GitVersion.MSBuildTask
{
    public abstract class ProxiedTask<TTask> : IProxiedTask
         where TTask : ProxiedTask<TTask>
    {
        public virtual bool Execute()
        { 
            var assyProvider = GetAssemblyProvider(); // different platforms load assemblies in different ways.
            var taskInstance = (TTask)this;
            var taskProxy = new TaskProxy<TTask>(assyProvider, taskInstance);
            return taskProxy.InvokeProxiedExecute();
        }

        public abstract IAssemblyProvider GetAssemblyProvider();

        public abstract bool OnProxyExecute();
        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

    }
}
