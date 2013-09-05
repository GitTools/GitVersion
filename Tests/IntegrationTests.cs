using System.Diagnostics;
using System.Linq;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class IntegrationTests
{

    [Test,Explicit]
    public void NServiceBusMaster()
    {
        using (var repository = new Repository(@"C:\Code\Particular\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "master");
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
    [Test,Explicit]
    public void NServiceBusMaster2()
    {
        using (var repository = new Repository(@"C:\Code\Particular\foo\NServiceBus"))
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
    [Test,Explicit]
    public void NServiceBusHotfix()
    {
        using (var repository = new Repository(@"C:\Code\Particular\NServiceBus"))
        {
            var branch = repository.Branches.First(x => x.Name == "hotfix-4.0.4");
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