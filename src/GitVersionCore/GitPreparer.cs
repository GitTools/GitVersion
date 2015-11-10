namespace GitVersion
{
    using System;
    using System.IO;
    using System.Linq;

    using GitVersion.Helpers;

    using LibGit2Sharp;

    public class GitPreparer
    {
        string targetUrl;
        string dynamicRepositoryLocation;
        Authentication authentication;
        bool noFetch;
        string targetPath;
        readonly IFileSystem fileSystem;


        public GitPreparer(string targetUrl, string dynamicRepositoryLocation, Authentication authentication, bool noFetch, string targetPath, IFileSystem fileSystem)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }

            this.targetUrl = targetUrl;
            this.dynamicRepositoryLocation = dynamicRepositoryLocation;
            this.authentication = authentication;
            this.noFetch = noFetch;
            this.targetPath = targetPath;
            this.fileSystem = fileSystem;
        }

        public bool IsDynamicGitRepository
        {
            get { return !string.IsNullOrWhiteSpace(DynamicGitRepositoryPath); }
        }

        public string DynamicGitRepositoryPath { get; private set; }

        public void Initialise(bool normaliseGitDirectory, string currentBranch)
        {
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                if (normaliseGitDirectory)
                {
                    GitHelper.NormalizeGitDirectory(GetDotGitDirectory(), authentication, noFetch, currentBranch);
                }
                return;
            }

            var tempRepositoryPath = CalculateTemporaryRepositoryPath(targetUrl, dynamicRepositoryLocation);

            DynamicGitRepositoryPath = CreateDynamicRepository(tempRepositoryPath, authentication, targetUrl, currentBranch, noFetch);
        }

        static string CalculateTemporaryRepositoryPath(string targetUrl, string dynamicRepositoryLocation)
        {
            var userTemp = dynamicRepositoryLocation ?? Path.GetTempPath();
            var repositoryName = targetUrl.Split('/', '\\').Last().Replace(".git", string.Empty);
            var possiblePath = Path.Combine(userTemp, repositoryName);

            // Verify that the existing directory is ok for us to use
            if (Directory.Exists(possiblePath))
            {
                if (!GitRepoHasMatchingRemote(possiblePath, targetUrl))
                {
                    var i = 1;
                    var originalPath = possiblePath;
                    bool possiblePathExists;
                    do
                    {
                        possiblePath = string.Concat(originalPath, "_", i++.ToString());
                        possiblePathExists = Directory.Exists(possiblePath);
                    } while (possiblePathExists && !GitRepoHasMatchingRemote(possiblePath, targetUrl));
                }
            }

            return possiblePath;
        }

        static bool GitRepoHasMatchingRemote(string possiblePath, string targetUrl)
        {
            try
            {
                using (var repository = new Repository(possiblePath))
                {
                    return repository.Network.Remotes.Any(r => r.Url == targetUrl);
                }
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        public string GetDotGitDirectory()
        {
            if (IsDynamicGitRepository)
                return DynamicGitRepositoryPath;

            return this.fileSystem.TreeWalkForDotGitDir(targetPath);
        }

        public string GetProjectRootDirectory()
        {
            if (IsDynamicGitRepository)
                return this.targetPath;

            var gitDir = fileSystem.TreeWalkForDotGitDir(this.targetPath);

            if (String.IsNullOrEmpty(gitDir))
                throw new DirectoryNotFoundException("Can't find the .git directory in " + targetPath);

            return Directory.GetParent(gitDir).FullName;
        }

        static string CreateDynamicRepository(string targetPath, Authentication authentication, string repositoryUrl, string targetBranch, bool noFetch)
        {
            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                throw new Exception("Dynamic Git repositories must have a target branch (/b)");
            }
            Logger.WriteInfo(string.Format("Creating dynamic repository at '{0}'", targetPath));

            var gitDirectory = Path.Combine(targetPath, ".git");
            if (Directory.Exists(targetPath))
            {
                Logger.WriteInfo("Git repository already exists");
                GitHelper.NormalizeGitDirectory(gitDirectory, authentication, noFetch, targetBranch);

                return gitDirectory;
            }

            CloneRepository(repositoryUrl, gitDirectory, authentication);

            // Normalize (download branches) before using the branch
            GitHelper.NormalizeGitDirectory(gitDirectory, authentication, noFetch, targetBranch);

            return gitDirectory;
        }

        private static void CloneRepository(string repositoryUrl, string gitDirectory, Authentication authentication)
        {
            Credentials credentials = null;
            if (!string.IsNullOrWhiteSpace(authentication.Username) && !string.IsNullOrWhiteSpace(authentication.Password))
            {
                Logger.WriteInfo(string.Format("Setting up credentials using name '{0}'", authentication.Username));

                credentials = new UsernamePasswordCredentials
                {
                    Username = authentication.Username,
                    Password = authentication.Password
                };
            }

            Logger.WriteInfo(string.Format("Retrieving git info from url '{0}'", repositoryUrl));

            try
            {
                Repository.Clone(repositoryUrl, gitDirectory,
                    new CloneOptions
                    {
                        Checkout = false,
                        CredentialsProvider = (url, usernameFromUrl, types) => credentials
                    });
            }
            catch (LibGit2SharpException ex)
            {
                var message = ex.Message;
                if (message.Contains("401"))
                {
                    throw new Exception("Unauthorised: Incorrect username/password");
                }
                if (message.Contains("403"))
                {
                    throw new Exception("Forbidden: Possbily Incorrect username/password");
                }
                if (message.Contains("404"))
                {
                    throw new Exception("Not found: The repository was not found");
                }
                
                throw new Exception("There was an unknown problem with the Git repository you provided");
            }
        }
    }
}