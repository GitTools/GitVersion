using Cake.Common.IO;
using Cake.Core.IO;
using Cake.Frosting;

public sealed class Clean : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        var globberSettings = new GlobberSettings
        {
            Predicate = x => !x.Path.FullPath.Contains("/build/")
        };

        var directories = context.GetDirectories("./**/bin", globberSettings)
                          + context.GetDirectories("./**/obj", globberSettings)
                          + context.Artifacts
                          + context.Packages
                          + context.CodeCoverage;

        foreach (var directory in directories)
        {
            context.CleanDirectory(directory);
        }
    }
}