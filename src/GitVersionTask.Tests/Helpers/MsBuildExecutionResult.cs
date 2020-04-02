using Microsoft.Build.Framework;

namespace GitVersionTask.Tests.Helpers
{
    public class MsBuildExecutionResult<T> where T : ITask
    {
        public bool Success { get; set; }

        public T Task { get; set; }

        public int Errors { get; set; }
        public int Warnings { get; set; }
        public int Messages { get; set; }
        public string Log { get; set; }
    }
}
