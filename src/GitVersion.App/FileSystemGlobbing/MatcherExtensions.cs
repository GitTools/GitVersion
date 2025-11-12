using System.IO.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing;

namespace GitVersion.FileSystemGlobbing;

internal static class MatcherExtensions
{
    extension(Matcher matcher)
    {
        public PatternMatchingResult Execute(IFileSystem fileSystem,
                                             string directoryPath
        )
        {
            ArgumentNullException.ThrowIfNull(matcher);
            ArgumentNullException.ThrowIfNull(fileSystem);

            return Execute(matcher, fileSystem, fileSystem.DirectoryInfo.New(directoryPath));
        }

        private PatternMatchingResult Execute(IFileSystem fileSystem,
                                              IDirectoryInfo directoryInfo
        )
        {
            ArgumentNullException.ThrowIfNull(matcher);
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(directoryInfo);

            return matcher.Execute(new DirectoryInfoGlobbingWrapper(fileSystem, directoryInfo));
        }

        public IEnumerable<string> GetResultsInFullPath(IFileSystem fileSystem,
                                                        string directoryPath
        )
        {
            ArgumentNullException.ThrowIfNull(matcher);
            ArgumentNullException.ThrowIfNull(fileSystem);

            return GetResultsInFullPath(
                matcher,
                fileSystem,
                fileSystem.DirectoryInfo.New(directoryPath)
            );
        }

        private IEnumerable<string> GetResultsInFullPath(IFileSystem fileSystem,
                                                         IDirectoryInfo directoryInfo
        )
        {
            ArgumentNullException.ThrowIfNull(matcher);
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(directoryInfo);

            var matches = Execute(matcher, fileSystem, directoryInfo);

            if (!matches.HasMatches)
            {
                return EmptyStringsEnumerable;
            }

            var fsPath = fileSystem.Path;
            var directoryFullName = directoryInfo.FullName;

            return matches.Files.Select(GetFullPath);

            string GetFullPath(FilePatternMatch match) =>
                fsPath.GetFullPath(fsPath.Combine(directoryFullName, match.Path));
        }
    }

    private static readonly IEnumerable<string> EmptyStringsEnumerable = [];
}
