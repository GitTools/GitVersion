namespace AcceptanceTests.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using GitVersion;
    using LibGit2Sharp;

    public static class GitHelper
    {
        public static Commit MakeACommit(this IRepository repository)
        {
            var randomFile = Path.Combine(repository.Info.WorkingDirectory, Guid.NewGuid().ToString());
            File.WriteAllText(randomFile, string.Empty);
            repository.Index.Stage(randomFile);
            return repository.Commit("Test Commit", Constants.SignatureNow());
        }
        public static void MergeNoFF(this IRepository repository, string branch, Signature sig)
        {
            repository.Merge(repository.FindBranch(branch).Tip, sig, new MergeOptions
            {
                FastForwardStrategy = FastForwardStrategy.NoFastFoward
            });
            repository.Commit(string.Format("Merge branch '{0}'", branch), amendPreviousCommit: true);
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
