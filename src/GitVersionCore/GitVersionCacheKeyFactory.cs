﻿namespace GitVersion
{
    using GitVersion.Helpers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public class GitVersionCacheKeyFactory
    {
        public static GitVersionCacheKey Create(IFileSystem fileSystem, GitPreparer gitPreparer, Config overrideConfig)
        {
            var gitSystemHash = GetGitSystemHash(gitPreparer, fileSystem);
            var configFileHash = GetConfigFileHash(fileSystem, gitPreparer);
            var repositorySnapshotHash = GetRepositorySnapshotHash(gitPreparer);
            var overrideConfigHash = GetOverrideConfigHash(overrideConfig);

            var compositeHash = GetHash(gitSystemHash, configFileHash, repositorySnapshotHash, overrideConfigHash);
            return new GitVersionCacheKey(compositeHash);
        }

        private static string GetGitSystemHash(GitPreparer gitPreparer, IFileSystem fileSystem)
        {
            var dotGitDirectory = gitPreparer.GetDotGitDirectory();

            // traverse the directory and get a list of files, use that for GetHash
            var traverse = TraverseTree(Path.Combine(dotGitDirectory, "refs"));

            return GetHash(traverse.ToArray());
        }

        // lifted from https://msdn.microsoft.com/en-us/library/bb513869.aspx
        public static List<string> TraverseTree(string root)
        {
            var result = new List<string>();
            result.Add(root);
            // Data structure to hold names of subfolders to be
            // examined for files.
            Stack<string> dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(root))
            {
                throw new ArgumentException();
            }

            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();

                var di = new DirectoryInfo(currentDir);
                result.Add(di.Name);

                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable 
                // to ignore the exception and continue enumerating the remaining files and 
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception 
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The 
                // choice of which exceptions to catch depends entirely on the specific task 
                // you are intending to perform and also on how much you know with certainty 
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException e)
                {
                    Logger.WriteError(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Logger.WriteError(e.Message);
                    continue;
                }

                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {
                    Logger.WriteError(e.Message);
                    continue;
                }

                catch (System.IO.DirectoryNotFoundException e)
                {
                    Logger.WriteError(e.Message);
                    continue;
                }
                // Perform the required action on each file here.
                // Modify this block to perform your required task.
                foreach (string file in files)
                {
                    try
                    {
                        // Perform whatever action is required in your scenario.
                        System.IO.FileInfo fi = new System.IO.FileInfo(file);
                        result.Add(fi.Name);
                        result.Add(File.ReadAllText(file));
                        //Logger.WriteInfo(string.Format("{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime));
                    }
                    catch (IOException e)
                    {
                        Logger.WriteError(e.Message);
                        continue;
                    }
                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                // push in reverse order
                for (int i = subDirs.Length - 1; i >= 0; i--)
                {
                    dirs.Push(subDirs[i]);
                }
            }

            return result;
        }

        private static string GetRepositorySnapshotHash(GitPreparer gitPreparer)
        {
            var repositorySnapshot = gitPreparer.WithRepository(repo => string.Join(":", repo.Head.CanonicalName, repo.Head.Tip.Sha));
            return GetHash(repositorySnapshot);
        }

        private static string GetOverrideConfigHash(Config overrideConfig)
        {
            if (overrideConfig == null)
            {
                return string.Empty;
            }

            // Doesn't depend on command line representation and 
            // includes possible changes in default values of Config per se.
            var stringBuilder = new StringBuilder();
            using (var stream = new StringWriter(stringBuilder))
            {
                ConfigSerialiser.Write(overrideConfig, stream);
                stream.Flush();
            }
            var configContent = stringBuilder.ToString();

            return GetHash(configContent);
        }

        private static string GetConfigFileHash(IFileSystem fileSystem, GitPreparer gitPreparer)
        {
            // will return the same hash even when config file will be moved 
            // from workingDirectory to rootProjectDirectory. It's OK. Config essentially is the same.
            var configFilePath = ConfigurationProvider.SelectConfigFilePath(gitPreparer, fileSystem);
            if (!fileSystem.Exists(configFilePath))
            {
                return string.Empty;
            }

            var configFileContent = fileSystem.ReadAllText(configFilePath);
            return GetHash(configFileContent);
        }

        static string GetHash(params string[] textsToHash)
        {
            var textToHash = string.Join(":", textsToHash);
            return GetHash(textToHash);
        }

        static string GetHash(string textToHash)
        {
            if (string.IsNullOrEmpty(textToHash))
            {
                return string.Empty;
            }

            using (var sha1 = SHA1.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(textToHash);
                var hashedBytes = sha1.ComputeHash(bytes);
                var hashedString = BitConverter.ToString(hashedBytes);
                return hashedString.Replace("-", "");
            }
        }
    }
}
