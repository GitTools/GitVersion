using System.Diagnostics;
using System.Linq;
using GitFlowVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class IntegrationTests
{


    [Test, Explicit]
    public void ProcessAllTheCommits()
    {
        using (var repository = new Repository(@"C:\Code\Particular\NServiceBus"))
        {
            foreach (var branch in repository.Branches)
            {

                foreach (var commit in branch.Commits)
                {
                    string versionPart;
                    if (MergeMessageParser.TryParse(commit.Message, out versionPart))
                    {
                        Debug.WriteLine(versionPart);
                        SemanticVersion version;
                        if (SemanticVersionParser.TryParse(versionPart, out version))
                        {
                            Debug.WriteLine("{0}.{1}.{2}.{3}.{4}.{5}", version.Major, version.Minor, version.Patch, version.Stability, version.PreReleaseNumber, version.Suffix);
                        }
                    }
                }
            }
        }
    }
    [Test, Explicit]
    public void TimingOnNSB()
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
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleaseNumber);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
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
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleaseNumber);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
    }

    [Test,Explicit]
    public void NServiceBusDevelop()
    {
        var stopwatch = Stopwatch.StartNew();
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
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleaseNumber);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
        Debug.WriteLine(stopwatch.ElapsedMilliseconds); stopwatch = Stopwatch.StartNew();
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
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleaseNumber);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
        Debug.WriteLine(stopwatch.ElapsedMilliseconds);
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
            Debug.WriteLine(version.Version.Major);
            Debug.WriteLine(version.Version.Minor);
            Debug.WriteLine(version.Version.Patch);
            Debug.WriteLine(version.Version.PreReleaseNumber);
            Debug.WriteLine(version.Version.Stability);
            Debug.WriteLine(version.BranchType);
            Debug.WriteLine(version.Version.Suffix);
        }
    }
}