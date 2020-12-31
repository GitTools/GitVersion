using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Restore;
using Cake.Frosting;

[IsDependentOn(typeof(Clean))]
public sealed class Restore : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.DotNetCoreRestore("./GitVersion.sln", new DotNetCoreRestoreSettings());
    }
}