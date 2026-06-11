using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace GitVersion.MsBuild.Tasks;

public abstract class GitVersionTaskBase : ITask
{
    public IBuildEngine BuildEngine { get; set; } = null!;
    public ITaskHost HostObject { get; set; } = null!;

    protected GitVersionTaskBase() => Log = new(this);

    [Required]
    public string SolutionDirectory { get; set; } = null!;

    public string VersionFile { get; set; } = null!;

    public TaskLoggingHelper Log { get; }

    public bool Execute() => GitVersionTasks.Execute(this);
}
