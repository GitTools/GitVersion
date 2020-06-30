using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace GitVersion
{
    public class GlobbingResolver : IGlobbingResolver
    {
        private Matcher matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

        public IEnumerable<string> Resolve(string workingDirectory, string pattern)
        {
            matcher.AddInclude(pattern);
            return matcher.Execute(GetDirectoryInfoWrapper(workingDirectory)).Files.Select(file => file.Path);
        }

        protected virtual DirectoryInfoBase GetDirectoryInfoWrapper(string workingDirectory) => new DirectoryInfoWrapper(new DirectoryInfo(workingDirectory));
    }
}
