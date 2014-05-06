// From https://github.com/libgit2/libgit2sharp/blob/f43d558/LibGit2Sharp.Tests/TestHelpers/BaseFixture.cs

    using System;
    using System.Collections.Generic;
    using System.IO;
    using LibGit2Sharp;
    using NUnit.Framework;

public abstract class Lg2sHelperBase : IPostTestDirectoryRemover
{
    List<string> directories;

    [TestFixtureSetUp]
    public void Setup()
    {
        directories = new List<string>();
    }

    [TestFixtureTearDown]
    public virtual void TearDown()
    {
        foreach (var directory in directories)
        {
            DirectoryHelper.DeleteDirectory(directory);
        }
    }

    static Lg2sHelperBase()
    {
        // Do the set up in the static ctor so it only happens once
        SetUpTestEnvironment();

        if (Directory.Exists(Constants.TemporaryReposPath))
        {
            DirectoryHelper.DeleteSubDirectories(Constants.TemporaryReposPath);
        }
    }

    protected static string ASBMTestRepoWorkingDirPath { private set; get; }
    protected static string CCTestRepoWorkingDirPath { private set; get; }
    static DirectoryInfo ResourcesDirectory;

    static void SetUpTestEnvironment()
    {
        var source = new DirectoryInfo(@"../../Resources");
        ResourcesDirectory = new DirectoryInfo(string.Format(@"Resources/{0}", Guid.NewGuid()));
        var parent = new DirectoryInfo(@"Resources");

        if (parent.Exists)
        {
            DirectoryHelper.DeleteSubDirectories(parent.FullName);
        }

        DirectoryHelper.CopyFilesRecursively(source, ResourcesDirectory);

        // Setup standard paths to our test repositories
        ASBMTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "asbm_wd");
        CCTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "commit_counting_wd");
    }

    protected SelfCleaningDirectory BuildSelfCleaningDirectory()
    {
        return new SelfCleaningDirectory(this);
    }

    protected SelfCleaningDirectory BuildSelfCleaningDirectory(string path)
    {
        return new SelfCleaningDirectory(this, path);
    }

    protected string Clone(string sourceDirectoryPath, params string[] additionalSourcePaths)
    {
        var scd = BuildSelfCleaningDirectory();
        var source = new DirectoryInfo(sourceDirectoryPath);

        var clonePath = Path.Combine(scd.DirectoryPath, source.Name);
        DirectoryHelper.CopyFilesRecursively(source, new DirectoryInfo(clonePath));

        foreach (var additionalPath in additionalSourcePaths)
        {
            var additional = new DirectoryInfo(additionalPath);
            var targetForAdditional = Path.Combine(scd.DirectoryPath, additional.Name);

            DirectoryHelper.CopyFilesRecursively(additional, new DirectoryInfo(targetForAdditional));
        }

        return clonePath;
    }

    protected string InitNewRepository(bool isBare = false)
    {
        var scd = BuildSelfCleaningDirectory();

        return Repository.Init(scd.DirectoryPath, isBare);
    }

    public void Register(string directoryPath)
    {
        directories.Add(directoryPath);
    }

    protected static Commit AddOneCommitToHead(Repository repo, string type)
    {
        var sign = Constants.SignatureNow();
        return repo.Commit(type + " commit", sign, sign);
    }
}