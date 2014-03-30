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

        public SemanticVersion GetNextVersion()
        {
            var filePath = Path.Combine(repositoryDirectory, "NextVersion.txt");
            if (!File.Exists(filePath))
            {
                return new SemanticVersion();
            }
            var version = File.ReadAllText(filePath);

            if (string.IsNullOrEmpty(version))
                return new SemanticVersion();

            SemanticVersion semanticVersion;
            if (!SemanticVersion.TryParse(version, out semanticVersion))
                throw new ArgumentException("Make sure you have a valid semantic version in NextVersion.txt");

            return semanticVersion;
        }
    }
}