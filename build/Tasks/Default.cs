using System;
using System.Linq;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;
using GitVersion.Build.Utilities;

namespace GitVersion.Build.Tasks
{
    [TaskName(nameof(Default))]
    [TaskDescription("Shows this output")]
    public class Default : FrostingTask
    {
        public override void Run(ICakeContext context)
        {
            var tasks = GetType().Assembly.FindAllDerivedTypes(typeof(IFrostingTask)).ToList();
            context.Information($"Available targets:{Environment.NewLine}");
            foreach (var task in tasks)
            {
                context.Information($"./build.ps1 --target={task.GetTaskName()} # ({task.GetTaskDescription()})");
            }
        }
    }
}
