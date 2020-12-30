using Cake.Common.Solution;
using Cake.Core.Diagnostics;
using Cake.Frosting;

[TaskName("Hello")]
public sealed class HelloTask : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.Log.Information("Hello");
    }
}