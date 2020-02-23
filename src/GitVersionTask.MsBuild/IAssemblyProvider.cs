using System.Reflection;

namespace GitVersion.MSBuildTask
{
    /// <summary>
    /// On different platforms, need to load assemblies in different ways.
    /// </summary>
    public interface IAssemblyProvider
    {
        Assembly GetAssembly(string tasksAssembly);
    }
}
