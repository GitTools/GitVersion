using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Frosting;

[IsDependentOn(typeof(Restore))]
public sealed class Build : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.DotNetCoreBuild("./GitVersion.sln", new DotNetCoreBuildSettings
        {
            Configuration = context.Configuration,
            NoRestore = true
        });
    }
}