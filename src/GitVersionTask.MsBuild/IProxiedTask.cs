using Microsoft.Build.Framework;

namespace GitVersion.MSBuildTask
{
    public interface IProxiedTask : ITask
    {
        bool OnProxyExecute();
    }
}
