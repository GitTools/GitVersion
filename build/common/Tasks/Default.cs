using System;
using System.Linq;
using System.Reflection;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;
using Common.Utilities;

namespace Common.Tasks
{
    [TaskName(nameof(Default))]
    [TaskDescription("Shows this output")]
    public class Default : FrostingTask
    {
        public override void Run(ICakeContext context)
        {
            var tasks = Assembly.GetEntryAssembly()?.FindAllDerivedTypes(typeof(IFrostingTask)).ToList();
            context.Information($"Available targets:{Environment.NewLine}");
            foreach (var task in tasks)
            {
                context.Information($"./build.ps1 --target={task.GetTaskName()} # ({task.GetTaskDescription()})");
            }
        }
    }
}
