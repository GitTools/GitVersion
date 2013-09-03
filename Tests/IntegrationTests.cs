using System.Diagnostics;
using System.Linq;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class IntegrationTests
{

    [Test,Explicit]
    public void NServiceBusDevelop()
    {
        using (var repository = new Repository(@"C:\Code\Particular\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "develop");
            var commit = branch.Commits.First();

            var finder = new GitFlowVersionFinder
                         {
                             Commit = commit,
                             Repository = repository,
                             Branch = branch
                         };
            var version = finder.FindVersion();
            Debug.WriteLine(version.Major);
            Debug.WriteLine(version.Minor);
            Debug.WriteLine(version.Patch);
            Debug.WriteLine(version.PreRelease);
            Debug.WriteLine(version.Stage);
            Debug.WriteLine(version.Suffix);
        }
    }
}