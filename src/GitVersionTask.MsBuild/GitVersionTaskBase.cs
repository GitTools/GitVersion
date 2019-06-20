namespace GitVersionTask.MsBuild
{
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    public abstract class GitVersionTaskBase : Task
    {
        [Required]
        public string SolutionDirectory { get; set; }

        public bool NoFetch { get; set; }
    }
}