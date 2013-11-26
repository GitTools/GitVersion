// From https://github.com/libgit2/libgit2sharp/blob/f43d558/LibGit2Sharp.Tests/TestHelpers/BaseFixture.cs

namespace Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using LibGit2Sharp;
    using NUnit.Framework;

    public abstract class Lg2sHelperBase : IPostTestDirectoryRemover
    {
        private List<string> directories;

        [TestFixtureSetUp]
        public void Steup()
        {
            directories = new List<string>();
        }

        [TestFixtureTearDown]
        public virtual void Teardown()
        {
            foreach (string directory in directories)
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
                DirectoryHelper.DeleteSubdirectories(Constants.TemporaryReposPath);
            }
        }

        public static string ASBMTestRepoWorkingDirPath { get; private set; }
        public static DirectoryInfo ResourcesDirectory { get; private set; }

        private static void SetUpTestEnvironment()
        {
            var source = new DirectoryInfo(@"../../Resources");
            ResourcesDirectory = new DirectoryInfo(string.Format(@"Resources/{0}", Guid.NewGuid()));
            var parent = new DirectoryInfo(@"Resources");

            if (parent.Exists)
            {
                DirectoryHelper.DeleteSubdirectories(parent.FullName);
            }

            DirectoryHelper.CopyFilesRecursively(source, ResourcesDirectory);

            // Setup standard paths to our test repositories
            ASBMTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "asbm_wd");
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
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            return Repository.Init(scd.DirectoryPath, isBare);
        }

        public void Register(string directoryPath)
        {
            directories.Add(directoryPath);
        }

        protected static void AddOneCommitToHead(Repository repo, string type)
        {
            var sign = Constants.SignatureNow();
            repo.Commit(type + " commit", sign, sign);
        }
    }

    public interface IPostTestDirectoryRemover
    {
        void Register(string directoryPath);
    }

    public class SelfCleaningDirectory
    {
        public SelfCleaningDirectory(IPostTestDirectoryRemover directoryRemover)
            : this(directoryRemover, BuildTempPath())
        {
        }

        public SelfCleaningDirectory(IPostTestDirectoryRemover directoryRemover, string path)
        {
            if (Directory.Exists(path))
            {
                throw new InvalidOperationException(string.Format("Directory '{0}' already exists.", path));
            }

            DirectoryPath = path;
            RootedDirectoryPath = Path.GetFullPath(path);

            directoryRemover.Register(DirectoryPath);
        }

        public string DirectoryPath { get; private set; }
        public string RootedDirectoryPath { get; private set; }

        protected static string BuildTempPath()
        {
            return Path.Combine(Constants.TemporaryReposPath, Guid.NewGuid().ToString().Substring(0, 8));
        }
    }

    public static class Constants
    {
        public const string TemporaryReposPath = "TestRepos";

        public static Signature SignatureNow()
        {
            return new Signature("A. U. Thor", "thor@valhalla.asgard.com", DateTimeOffset.Now);
        }
    }


    public static class DirectoryHelper
    {
        private static readonly Dictionary<string, string> toRename = new Dictionary<string, string>
            {
                {"gitted", ".git"},
                {"gitmodules", ".gitmodules"},
            };

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            // From http://stackoverflow.com/questions/58744/best-way-to-copy-the-entire-contents-of-a-directory-in-c/58779#58779

            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                CopyFilesRecursively(dir, target.CreateSubdirectory(Rename(dir.Name)));
            }
            foreach (FileInfo file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, Rename(file.Name)));
            }
        }

        private static string Rename(string name)
        {
            return toRename.ContainsKey(name) ? toRename[name] : name;
        }

        public static void DeleteSubdirectories(string parentPath)
        {
            string[] dirs = Directory.GetDirectories(parentPath);
            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }
        }

        public static void DeleteDirectory(string directoryPath)
        {
            // From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502

            if (!Directory.Exists(directoryPath))
            {
                Trace.WriteLine(
                    string.Format("Directory '{0}' is missing and can't be removed.",
                        directoryPath));

                return;
            }

            string[] files = Directory.GetFiles(directoryPath);
            string[] dirs = Directory.GetDirectories(directoryPath);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            File.SetAttributes(directoryPath, FileAttributes.Normal);
            try
            {
                Directory.Delete(directoryPath, false);
            }
            catch (IOException)
            {
                Trace.WriteLine(string.Format("{0}The directory '{1}' could not be deleted!" +
                                              "{0}Most of the time, this is due to an external process accessing the files in the temporary repositories created during the test runs, and keeping a handle on the directory, thus preventing the deletion of those files." +
                                              "{0}Known and common causes include:" +
                                              "{0}- Windows Search Indexer (go to the Indexing Options, in the Windows Control Panel, and exclude the bin folder of LibGit2Sharp.Tests)" +
                                              "{0}- Antivirus (exclude the bin folder of LibGit2Sharp.Tests from the paths scanned by your real-time antivirus){0}",
                    Environment.NewLine, Path.GetFullPath(directoryPath)));
            }
        }
    }
}
