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
            var entryAssembly = Assembly.GetEntryAssembly();
            var tasks = entryAssembly?.FindAllDerivedTypes(typeof(IFrostingTask)).ToList();
            if (tasks == null) return;
            context.Information($"Available targets:{Environment.NewLine}");
            foreach (var task in tasks)
            {
                context.Information($"./build.ps1 --stage={entryAssembly?.GetName().Name} --target={task.GetTaskName()} # ({task.GetTaskDescription()})");
            }
        }
    }
}
