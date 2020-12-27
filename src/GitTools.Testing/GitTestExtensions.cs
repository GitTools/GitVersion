using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace GitTools.Testing
{
    public static class GitTestExtensions
    {
        static int _pad = 1;

        public static Commit MakeACommit(this IRepository repository, string commitMessage = null)
        {
            return CreateFileAndCommit(repository, Guid.NewGuid().ToString(), commitMessage);
        }

        public static void MergeNoFF(this IRepository repository, string branch)
        {
            MergeNoFF(repository, branch, Generate.SignatureNow());
        }

        public static void MergeNoFF(this IRepository repository, string branch, Signature sig)
        {
            repository.Merge(repository.Branches[branch], sig, new MergeOptions
            {
                FastForwardStrategy = FastForwardStrategy.NoFastForward
            });
        }

        public static Commit[] MakeCommits(this IRepository repository, int numCommitsToMake)
        {
            return Enumerable.Range(1, numCommitsToMake)
                             .Select(x => repository.MakeACommit())
                             .ToArray();
        }

        public static Commit CreateFileAndCommit(this IRepository repository, string relativeFileName, string commitMessage = null)
        {
            var randomFile = Path.Combine(repository.Info.WorkingDirectory, relativeFileName);
            if (File.Exists(randomFile))
            {
                File.Delete(randomFile);
            }

            var totalWidth = 36 + (_pad++ % 10);
            var contents = Guid.NewGuid().ToString().PadRight(totalWidth, '.');
            File.WriteAllText(randomFile, contents);

            Commands.Stage(repository, randomFile);

            return repository.Commit(string.Format("Test Commit for file '{0}' - {1}", relativeFileName, commitMessage),
                Generate.SignatureNow(), Generate.SignatureNow());
        }

        public static Tag MakeATaggedCommit(this IRepository repository, string tag)
        {
            var commit = repository.MakeACommit();
            var existingTag = repository.Tags.SingleOrDefault(t => t.FriendlyName == tag);
            if (existingTag != null)
                return existingTag;
            return repository.Tags.Add(tag, commit);
        }

        public static Commit CreatePullRequestRef(this IRepository repository, string from, string to, int prNumber = 2, bool normalise = false, bool allowFastFowardMerge = false)
        {
            Commands.Checkout(repository, repository.Branches[to].Tip);
            if (allowFastFowardMerge)
            {
                repository.Merge(repository.Branches[from], Generate.SignatureNow());
            }
            else
            {
                repository.MergeNoFF(from);
            }
            var commit = repository.Head.Tip;
            repository.Refs.Add("refs/pull/" + prNumber + "/merge", commit.Id);
            Commands.Checkout(repository, to);
            if (normalise)
            {
                // Turn the ref into a real branch
                Commands.Checkout(repository, repository.Branches.Add("pull/" + prNumber + "/merge", commit));
            }

            return commit;
        }
    }
}