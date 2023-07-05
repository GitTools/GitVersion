using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.MsBuild;

public abstract class GitVersionTaskBase : ITask
{
    public IBuildEngine BuildEngine { get; set; }
    public ITaskHost HostObject { get; set; }

    protected GitVersionTaskBase() => Log = new TaskLoggingHelper(this);

    [Required]
    public string SolutionDirectory { get; set; }

    public string VersionFile { get; set; }


    public TaskLoggingHelper Log { get; }

    public bool Execute() => OnExecute();

    protected abstract bool OnExecute();

    public Action<IServiceCollection>? Overrides { get; set; }

    public void WithOverrides(Action<IServiceCollection> overrides) => Overrides = overrides;
}
