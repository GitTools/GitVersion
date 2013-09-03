namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using LibGit2Sharp;

    static class LibGitExtensions
    {
        static FieldInfo repoField;

        static LibGitExtensions()
        {
            repoField = typeof (Branch).GetField("repo", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
        }
        public static Repository Repository(this Branch branch)
        {
            return (Repository)repoField.GetValue(branch);
        }
        public static IEnumerable<Tag> CommitTags(this Repository repository)
        {
            return repository.Tags.Where(tag => tag.Target is Commit);
        }
        public static IEnumerable<Reference> LocalBranchRefs(this Repository repository)
        {
            return repository.Refs.Where(r => r.IsLocalBranch());
        }
        public static IEnumerable<Reference> TagRefs(this Repository repository)
        {
            return repository.Refs.Where(r => r.IsTag());
        }
        public static Reference ToReference(this Branch branch)
        {
            return branch.Repository().Refs.First(x => x.CanonicalName == branch.CanonicalName);
        }
        public static bool IsOnBranch( this Commit commit,  Branch branch)
        {
             return branch.Repository().Refs.ReachableFrom(new[] { branch.ToReference() }, new[] { commit }).Any();
        }
        public static bool IsOnBranch( this Tag tag,  Branch branch)
        {
            var commit = tag.Target as Commit;
            if (commit == null)
            {
                return false;
            }
            return commit.IsOnBranch(branch);
        }
        public static DateTimeOffset CommitTimeStamp( this Tag tag)
        {
            var commit = tag.Target as Commit;
            if (commit == null)
            {
                throw new InvalidOperationException();
            }
            return commit.Committer.When;
        }
        public static bool IsBefore( this Tag tag, Commit commit)
        {
            var tagCommit = tag.Target as Commit;
            if (tagCommit == null)
            {
                throw new InvalidOperationException();
            }
            return tagCommit.Committer.When <=  commit.Committer.When;
        }


    }
}