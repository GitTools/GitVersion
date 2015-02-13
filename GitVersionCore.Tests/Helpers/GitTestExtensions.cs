using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GitVersion;
using GitVersion.Helpers;
using LibGit2Sharp;

public static class GitTestExtensions
{
    public static Commit MakeACommit(this IRepository repository)
    {
        return MakeACommit(repository, Constants.Now);
    }

    public static void DumpGraph(this IRepository repository)
    {
        var output = new StringBuilder();

        ProcessHelper.Run(
            o => output.AppendLine(o),
            e => output.AppendLineFormat("ERROR: {0}", e),
            null,
            "git",
            @"log --graph --abbrev-commit --decorate --date=relative --all",
            repository.Info.Path);

        Trace.Write(output.ToString());
    }

    public static Commit MakeACommit(this IRepository repository, DateTimeOffset dateTimeOffset)
    {
        var randomFile = Path.Combine(repository.Info.WorkingDirectory, Guid.NewGuid().ToString());
        File.WriteAllText(randomFile, string.Empty);
        repository.Stage(randomFile);
        return repository.Commit("Test Commit", Constants.Signature(dateTimeOffset), Constants.Signature(dateTimeOffset));
    }

    public static void MergeNoFF(this IRepository repository, string branch)
    {
        MergeNoFF(repository, branch, Constants.SignatureNow());
    }

    public static void MergeNoFF(this IRepository repository, string branch, Signature sig)
    {
        // Fixes a race condition
        repository.Merge(repository.FindBranch(branch), sig, new MergeOptions
        {
            FastForwardStrategy = FastForwardStrategy.NoFastFoward
        });
    }

    public static Commit[] MakeCommits(this IRepository repository, int numCommitsToMake)
    {
        return Enumerable.Range(1, numCommitsToMake)
            .Select(x => repository.MakeACommit())
            .ToArray();
    }

    public static Tag MakeATaggedCommit(this IRepository repository, string tag)
    {
        var commit = repository.MakeACommit();
        var existingTag = repository.Tags.SingleOrDefault(t => t.Name == tag);
        if (existingTag != null)
            return existingTag;
        return repository.Tags.Add(tag, commit);
    }
}