using System.Linq;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Frosting;

[TaskName("Build")]
public sealed class BuildTask : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        var sln = context.GetFiles("*.sln").Single();
        context.DotNetCoreBuild(sln.FullPath);
    }
}