namespace Tests
{
    using System.Linq;
    using GitVersion;
    using Microsoft.Build.Framework;
    using NUnit.Framework;

    public class GetVersionTaskTests
    {
        [Test]
        public void OutputsShouldMatchVariableProvider()
        {
            var taskType = typeof(GitVersionTask.GetVersion);
            var properties = taskType.GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(OutputAttribute), false).Any())
                .Select(p => p.Name);
            var variables = new VersionAndBranch
            {
                BranchName = "master",
                Version = new SemanticVersion
                {
                    Major = 1,
                    Minor = 2,
                    Patch = 3
                }
            }.ToKeyValue().Keys;

            CollectionAssert.AreEquivalent(properties, variables);
        }
    }
}