using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace GitHubFlowVersion.AcceptanceTests.Helpers
{
    public static class GitHelper
    {
        public static Commit MakeACommit(this IRepository repository)
        {
            var randomFile = Path.Combine(repository.Info.WorkingDirectory, Guid.NewGuid().ToString());
            File.WriteAllText(randomFile, string.Empty);
            repository.Index.Stage(randomFile);
            return repository.Commit("Test Commit", new Signature("Test User", "test@email.com", DateTimeOffset.UtcNow));
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
            var existingTag = repository.Tags.SingleOrDefault(t=>t.Name == tag);
            if (existingTag != null)
                return existingTag;
            return repository.Tags.Add(tag, commit);
        }
    }
}
