namespace AcceptanceTests
{
    using System;
    using System.IO;
    using LibGit2Sharp;

    public class CommitCountingRepoFixture : RepositoryFixtureBase
    {
        public CommitCountingRepoFixture() :
            base(CloneTestRepo)
        { }

        static IRepository CloneTestRepo(string path)
        {
            const string repoName = "commit_counting_wd";
            var source = new DirectoryInfo(@"../../Resources/" + repoName);
            DirectoryHelper.CopyFilesRecursively(source, new DirectoryInfo(path));
            Console.WriteLine("Cloned git repository '{0}' at '{1}'", repoName, path);

            return new Repository(path);
        }
    }
}
