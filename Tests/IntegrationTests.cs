using System.Diagnostics;
using System.Linq;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class IntegrationTests
{


    [Test, Explicit]
    public void NServiceBusMaster()
    {
        var startNew = Stopwatch.StartNew();
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
        }
        Debug.WriteLine(startNew.ElapsedMilliseconds);
        startNew = Stopwatch.StartNew();
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
        }
        Debug.WriteLine(startNew.ElapsedMilliseconds);
    }
    
    [Test,Explicit]
    public void DirectoryDateFinderTest()
    {
        var stopwatch = Stopwatch.StartNew();
        DirectoryDateFinder.GetLastDirectoryWrite(@"C:\Code\Particular\NServiceBus\.git");
        Debug.WriteLine(stopwatch.ElapsedMilliseconds);
        stopwatch = Stopwatch.StartNew();
        DirectoryDateFinder.GetLastDirectoryWrite(@"C:\Code\Particular\NServiceBus\.git");
        Debug.WriteLine(stopwatch.ElapsedMilliseconds);
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
            Debug.WriteLine(version.PreReleaseNumber);
            Debug.WriteLine(version.Stability);
            Debug.WriteLine(version.BranchType);
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
            Debug.WriteLine(version.PreReleaseNumber);
            Debug.WriteLine(version.Stability);
            Debug.WriteLine(version.BranchType);
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
            Debug.WriteLine(version.PreReleaseNumber);
            Debug.WriteLine(version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Suffix);
        }
    }
    [Test,Explicit]
    public void Foo()
    {
        using (var repository = new Repository(@"C:\Code\Particular\ServicePulse"))
        {
            var branch = repository.Branches.First(x => x.Name == "feature-newUI");
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
            Debug.WriteLine(version.PreReleaseNumber);
            Debug.WriteLine(version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Suffix);
        }
    }
}