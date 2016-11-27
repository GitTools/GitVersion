﻿namespace GitVersion
{
    using Helpers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Linq;

    class GitVersionCacheKeyFactory
    {
        public static GitVersionCacheKey Create(IFileSystem fileSystem, GitPreparer gitPreparer, Config overrideConfig)
        {
            var gitSystemHash = GetGitSystemHash(gitPreparer);
            var configFileHash = GetConfigFileHash(fileSystem, gitPreparer);
            var repositorySnapshotHash = GetRepositorySnapshotHash(gitPreparer);
            var overrideConfigHash = GetOverrideConfigHash(overrideConfig);

            var compositeHash = GetHash(gitSystemHash, configFileHash, repositorySnapshotHash, overrideConfigHash);
            return new GitVersionCacheKey(compositeHash);
        }

        static string GetGitSystemHash(GitPreparer gitPreparer)
        {
            var dotGitDirectory = gitPreparer.GetDotGitDirectory();

            // traverse the directory and get a list of files, use that for GetHash
            var contents = CalculateDirectoryContents(Path.Combine(dotGitDirectory, "refs"));

            return GetHash(contents.ToArray());
        }

        // based on https://msdn.microsoft.com/en-us/library/bb513869.aspx
        static List<string> CalculateDirectoryContents(string root)
        {
            var result = new List<string>();

            // Data structure to hold names of subfolders to be
            // examined for files.
            var dirs = new Stack<string>();

            if (!Directory.Exists(root))
            {
                throw new ArgumentException();
            }

            dirs.Push(root);

            while (dirs.Any())
            {
                string currentDir = dirs.Pop();

                var di = new DirectoryInfo(currentDir);
                result.Add(di.Name);

                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
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
                catch (DirectoryNotFoundException e)
                {
                    Logger.WriteError(e.Message);
                    continue;
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException e)
                {
                    Logger.WriteError(e.Message);
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    Logger.WriteError(e.Message);
                    continue;
                }

                foreach (var file in files)
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        result.Add(fi.Name);
                        result.Add(File.ReadAllText(file));
                    }
                    catch (IOException e)
                    {
                        Logger.WriteError(e.Message);
                    }
                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                // push in reverse order
                for (var i = subDirs.Length - 1; i >= 0; i--)
                {
                    dirs.Push(subDirs[i]);
                }
            }

            return result;
        }

        private static string GetRepositorySnapshotHash(GitPreparer gitPreparer)
        {
            var repositorySnapshot = gitPreparer.WithRepository(repo => {
                var head = repo.Head;
                if (head.Tip == null)
                {
                    return head.CanonicalName;
                }
                var hash = string.Join(":", head.CanonicalName, head.Tip.Sha);
                return hash;
            });
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
