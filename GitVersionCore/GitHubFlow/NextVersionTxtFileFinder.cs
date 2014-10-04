namespace GitVersion
{
    using System;
    using System.IO;

    public class NextVersionTxtFileFinder
    {
        string repositoryDirectory;

        public NextVersionTxtFileFinder(string repositoryDirectory)
        {
            this.repositoryDirectory = repositoryDirectory;
        }

        public bool TryGetNextVersion(out SemanticVersion semanticVersion)
        {
            var filePath = Path.Combine(repositoryDirectory, "NextVersion.txt");
            if (!File.Exists(filePath))
            {
                semanticVersion = null;
                return false;
            }

            var version = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(version))
            {
                semanticVersion = null;
                return false;
            }

            if (!SemanticVersion.TryParse(version, out semanticVersion))
            {
                throw new ArgumentException("Make sure you have a valid semantic version in NextVersion.txt");
            }


                return true;
        }
    }
}