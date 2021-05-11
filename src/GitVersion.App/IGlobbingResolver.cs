using System.Collections.Generic;

namespace GitVersion
{
    public interface IGlobbingResolver
    {
        public IEnumerable<string> Resolve(string workingDirectory, string pattern);
    }
}
