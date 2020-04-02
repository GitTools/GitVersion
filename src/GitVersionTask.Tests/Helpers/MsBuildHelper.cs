using Microsoft.Build.Framework;

namespace GitVersionTask.Tests.Helpers
{
    public class MsBuildHelper
    {
        public static MsBuildExecutionResult<T> Execute<T>(T task) where T : ITask
        {
            var buildEngine = new MockEngine();

            task.BuildEngine = buildEngine;

            var result = task.Execute();

            return new MsBuildExecutionResult<T>
            {
                Success = result,
                Task = task,
                Errors = buildEngine.Errors,
                Warnings = buildEngine.Warnings,
                Messages = buildEngine.Messages,
                Log = buildEngine.Log,
            };
        }
    }
}
