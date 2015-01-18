using System;
using System.Text;
using GitVersion;
using GitVersion.Helpers;
using LibGit2Sharp;

public class EmptyRepositoryFixture : RepositoryFixtureBase
{
    public EmptyRepositoryFixture(Config configuration) :
        base(CreateNewRepository, configuration)
    {
    }

    public void DumpGraph()
    {
        var output = new StringBuilder();

        ProcessHelper.Run(
            o => output.AppendLine(o),
            e => output.AppendLineFormat("ERROR: {0}", e),
            null,
            "git",
            @"log --graph --abbrev-commit --decorate --date=relative --all",
            RepositoryPath);

        Console.Write(output.ToString());
    }

    static IRepository CreateNewRepository(string path)
    {
        LibGit2Sharp.Repository.Init(path);
        Console.WriteLine("Created git repository at '{0}'", path);

        return new Repository(path);
    }
}