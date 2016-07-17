namespace GitVersion
{
    using GitVersion.Helpers;
    using System;
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

            // Maybe using timestamp in .git/refs directory is enough?
            var lastGitRefsChangedTicks = fileSystem.GetLastDirectoryWrite(Path.Combine(dotGitDirectory, "refs"));

            return GetHash(dotGitDirectory, lastGitRefsChangedTicks.ToString());
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
